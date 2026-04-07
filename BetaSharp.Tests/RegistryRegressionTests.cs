using BetaSharp.Registries;

namespace BetaSharp.Tests;

/// <summary>
/// Regression tests for bugs discovered during development of the registry infrastructure.
/// Each test documents a specific bug, its root cause, and the fix. If a fix is ever
/// accidentally reverted, the corresponding test will fail with a clear description.
/// </summary>
[Collection("RegistryAccess")]
public class RegistryRegressionTests : IDisposable
{
    private readonly string _tempDir;

    private static readonly RegistryKey<TestEnchantment> s_enchKey =
        new(ResourceLocation.Parse("test:enchantment"));

    public RegistryRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        RegistryAccess.ClearDynamicEntries();
    }

    public void Dispose()
    {
        RegistryAccess.ClearDynamicEntries();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private void WriteBaseEnchantment(string name, int maxLevel)
    {
        string dir = Path.Combine(_tempDir, "assets", "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"), $"{{\"MaxLevel\":{maxLevel}}}");
    }

    private void WriteWorldEnchantment(string packName, string name, int maxLevel)
    {
        string dir = Path.Combine(_tempDir, "world", "datapacks", packName, "data", "betasharp", "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"), $"{{\"MaxLevel\":{maxLevel}}}");
    }

    private void RegisterEnchantment()
    {
        RegistryAccess.AddDynamic(new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment"));
    }

    // -------------------------------------------------------------------------
    // Bug: CloneForWorldDatapacks shared Holder<T> references between the clone
    //      and the original server-level loader. A world-datapack override calls
    //      Holder.Value = newValue, which mutated the SAME holder object held by
    //      _serverLoaders, corrupting the server-level registry.
    //
    // Fix: CloneForWorldDatapacks now creates an independent Holder<T> for each
    //      entry, so world-pack writes cannot reach the originals.
    // -------------------------------------------------------------------------

    [Fact]
    public void World_datapack_override_does_not_corrupt_server_level_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("pack", "sharpness", maxLevel: 99);

        RegisterEnchantment();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        _ = serverRa.WithWorldDatapacks(Path.Combine(_tempDir, "world"));

        // Before the fix: the server registry also returned 99 because the
        // world-datapack write mutated the shared Holder<T>.
        int serverLevel = serverRa.GetOrThrow(s_enchKey)
            .GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel;

        Assert.Equal(5, serverLevel);
    }

    // -------------------------------------------------------------------------
    // Bug: RegistryDefinition.CreateLoader() used allowUnhandled=true (the default),
    //      which stores lazy Holder<T> closures that read from disk on first access.
    //      If the underlying file changed between Build() and first access, the old
    //      RegistryAccess would silently return new-file data instead of its own
    //      snapshot — violating the immutability contract.
    //
    // Fix: RegistryDefinition.CreateLoader() now passes allowUnhandled: false,
    //      forcing immediate deserialization at Build() time so each RegistryAccess
    //      is a true point-in-time snapshot.
    // -------------------------------------------------------------------------

    [Fact]
    public void RegistryAccess_is_a_snapshot_file_changes_after_build_are_not_visible()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        RegisterEnchantment();
        RegistryAccess snapshot = RegistryAccess.Build(basePath: _tempDir);

        // File changes after the build must not be visible through the old instance.
        WriteBaseEnchantment("sharpness", maxLevel: 99);
        _ = RegistryAccess.Build(basePath: _tempDir);

        // Before the fix: the lazy holder in `snapshot` would read the updated file
        // on first access and return 99.
        int level = snapshot.GetOrThrow(s_enchKey)
            .GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel;

        Assert.Equal(5, level);
    }

    // -------------------------------------------------------------------------
    // Bug: DataAssetLoader<T>.GetEnumerator() had an `if (h.IsResolved)` guard that
    //      silently skipped lazy (not-yet-accessed) holders. Since all entries from
    //      a fresh Build() start as lazy with allowUnhandled=true, iterating the
    //      registry would yield nothing, causing GameModes.SetDefaultGameMode to
    //      call registry.First() and throw InvalidOperationException.
    //
    // Fix: GetEnumerator() now calls h.Value unconditionally, resolving lazy entries
    //      on demand during enumeration.
    // -------------------------------------------------------------------------

    [Fact]
    public void Enumerating_registry_yields_all_entries_not_just_previously_accessed_ones()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteBaseEnchantment("fortune", maxLevel: 3);

        RegisterEnchantment();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        // Deliberately do NOT call Get() on any entry first — the bug only
        // manifested when entries had never been individually accessed.
        var names = ra.GetOrThrow(s_enchKey).Select(e => e.Name).OrderBy(n => n).ToList();

        // Before the fix: names was empty because all holders were unresolved.
        Assert.Equal(["fortune", "sharpness"], names);
    }

    // -------------------------------------------------------------------------
    // Bug: The default-game-mode fallback called registry.First() as last resort.
    //      With allowUnhandled=true loaders, no holders were resolved before that
    //      line, so GetEnumerator() yielded nothing and First() threw
    //      InvalidOperationException: "Sequence contains no elements."
    //
    // Fix: BetaSharpServer.ResolveDefaultGameMode now uses
    //      registry.Keys.FirstOrDefault() + registry.GetValue() so it never depends
    //      on GetEnumerator() resolving lazy entries.
    // -------------------------------------------------------------------------

    [Fact]
    public void ResolveDefaultGameMode_fallback_to_first_does_not_throw_before_any_entry_is_accessed()
    {
        string dir = Path.Combine(_tempDir, "assets", "gamemode");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "adventure.json"), "{}");

        RegistryAccess.AddDynamic(RegistryDefinitions.GameModes);
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);
        var registry = ra.GetOrThrow(RegistryKeys.GameModes);

        // Before the fix: this threw InvalidOperationException because
        // no GameMode holder had been resolved yet and First() yielded nothing.
        Holder<BetaSharp.GameMode>? result = null;
        var ex = Record.Exception(() => result = DefaultGameModeListener.ResolveDefaultGameMode(registry, ""));

        Assert.Null(ex);
        Assert.Equal("adventure", result!.Value.Name);
    }

    // -------------------------------------------------------------------------
    // Bug: Two independent WithWorldDatapacks() calls on the same server-level
    //      RegistryAccess shared the underlying _serverLoaders Holder<T> objects.
    //      Because CloneForWorldDatapacks copied holder references, a write from
    //      world-pack A would be visible in world-pack B's registry.
    //
    // Fix: Same as the first bug — independent holders per clone mean writes to
    //      one world's clone are invisible to any other world's clone.
    // -------------------------------------------------------------------------

    [Fact]
    public void Two_world_registries_from_same_server_registry_are_fully_independent()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        string worldADir = Path.Combine(_tempDir, "worldA");
        string worldBDir = Path.Combine(_tempDir, "worldB");
        string packA = Path.Combine(worldADir, "datapacks", "pack", "data", "betasharp", "enchantment");
        string packB = Path.Combine(worldBDir, "datapacks", "pack", "data", "betasharp", "enchantment");
        Directory.CreateDirectory(packA);
        Directory.CreateDirectory(packB);
        File.WriteAllText(Path.Combine(packA, "sharpness.json"), "{\"MaxLevel\":10}");
        File.WriteAllText(Path.Combine(packB, "sharpness.json"), "{\"MaxLevel\":20}");

        RegisterEnchantment();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess raA = serverRa.WithWorldDatapacks(worldADir);
        RegistryAccess raB = serverRa.WithWorldDatapacks(worldBDir);

        int levelA = raA.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel;
        int levelB = raB.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel;

        // Before the fix: one world's write would bleed into the other.
        Assert.Equal(10, levelA);
        Assert.Equal(20, levelB);
    }
}
