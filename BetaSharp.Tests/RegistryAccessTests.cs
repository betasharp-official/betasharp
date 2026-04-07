using BetaSharp.Registries;
using BetaSharp.Registries.Data;

namespace BetaSharp.Tests;

/// <summary>
/// A custom data-driven type modelling a Minecraft enchantment — used to verify that
/// the registry infrastructure works for any <see cref="IDataAsset"/> type, not just
/// the built-in <see cref="BetaSharp.GameMode.GameMode"/>.
/// </summary>
public class TestEnchantment : DataAsset
{
    /// <summary>Maximum level the enchantment can reach (e.g. Sharpness V → 5).</summary>
    public int MaxLevel { get; set; } = 1;

    /// <summary>Whether the enchantment can appear on books.</summary>
    public bool AllowedOnBooks { get; set; } = true;

    /// <summary>The rarity group, e.g. "common", "uncommon", "rare", "very_rare".</summary>
    public string Rarity { get; set; } = "common";
}

// Tests that modify RegistryAccess.s_dynamicEntries must not run in parallel with each other.
[Collection("RegistryAccess")]
public class RegistryAccessTests : IDisposable
{
    private readonly string _tempDir;

    // Well-known key for the test enchantment registry.
    private static readonly RegistryKey<TestEnchantment> s_enchKey =
        new(ResourceLocation.Parse("test:enchantment"));

    public RegistryAccessTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);

        // Ensure a clean dynamic-entry list for each test.
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

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Writes a JSON enchantment file under base assets.</summary>
    private void WriteBaseEnchantment(string name, int maxLevel, string rarity = "common", bool allowedOnBooks = true)
    {
        string dir = Path.Combine(_tempDir, "assets", "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"),
            $"{{\"MaxLevel\":{maxLevel},\"AllowedOnBooks\":{allowedOnBooks.ToString().ToLower()},\"Rarity\":\"{rarity}\"}}");
    }

    /// <summary>Writes a JSON enchantment file inside a named datapack.</summary>
    private void WriteDatapackEnchantment(string packName, string ns, string name, int maxLevel, string rarity = "common")
    {
        string dir = Path.Combine(_tempDir, "datapacks", packName, "data", ns, "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"),
            $"{{\"MaxLevel\":{maxLevel},\"Rarity\":\"{rarity}\"}}");
    }

    /// <summary>Writes a JSON enchantment file inside a world datapack.</summary>
    private void WriteWorldEnchantment(string packName, string ns, string name, int maxLevel)
    {
        string dir = Path.Combine(_tempDir, "world", "datapacks", packName, "data", ns, "enchantment");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{name}.json"),
            $"{{\"MaxLevel\":{maxLevel}}}");
    }

    private RegistryDefinition<TestEnchantment> RegisterEnchantmentDefinition(
        LoadLocations locations = LoadLocations.AllData)
    {
        var def = new RegistryDefinition<TestEnchantment>(s_enchKey, "enchantment", locations);
        RegistryAccess.AddDynamic(def);
        return def;
    }

    // -------------------------------------------------------------------------
    // RegistryAccess.Empty
    // -------------------------------------------------------------------------

    [Fact]
    public void Empty_returns_null_for_unknown_key()
    {
        IReadableRegistry<TestEnchantment>? reg = RegistryAccess.Empty.Get(s_enchKey);
        Assert.Null(reg);
    }

    [Fact]
    public void Empty_GetOrThrow_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RegistryAccess.Empty.GetOrThrow(s_enchKey));
    }

    // -------------------------------------------------------------------------
    // RegistryAccess.Build() — base assets
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_discovers_registered_definitions_and_loads_base_assets()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5, rarity: "common");
        WriteBaseEnchantment("silk_touch", maxLevel: 1, rarity: "very_rare");

        RegisterEnchantmentDefinition();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);

        TestEnchantment? sharpness = reg.GetValue(ResourceLocation.Parse("betasharp:sharpness"));
        TestEnchantment? silkTouch = reg.GetValue(ResourceLocation.Parse("betasharp:silk_touch"));

        Assert.NotNull(sharpness);
        Assert.Equal(5, sharpness.MaxLevel);
        Assert.Equal("common", sharpness.Rarity);

        Assert.NotNull(silkTouch);
        Assert.Equal(1, silkTouch.MaxLevel);
        Assert.Equal("very_rare", silkTouch.Rarity);
    }

    [Fact]
    public void Build_with_no_definitions_returns_empty_registry_access()
    {
        // No AddDynamic calls.
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);
        Assert.Null(ra.Get(s_enchKey));
    }

    [Fact]
    public void Build_does_not_include_unregistered_definition_in_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        // Deliberately do NOT call RegisterEnchantmentDefinition.

        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        Assert.Null(ra.Get(s_enchKey));
    }

    // -------------------------------------------------------------------------
    // RegistryAccess — IReadableRegistry<T> surface on DataAssetLoader<T>
    // -------------------------------------------------------------------------

    [Fact]
    public void Registry_Keys_contains_loaded_resource_locations()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteBaseEnchantment("fortune", maxLevel: 3);

        RegisterEnchantmentDefinition();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);
        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);

        Assert.Contains(ResourceLocation.Parse("betasharp:sharpness"), reg.Keys);
        Assert.Contains(ResourceLocation.Parse("betasharp:fortune"), reg.Keys);
    }

    [Fact]
    public void Registry_ContainsKey_returns_true_for_loaded_entry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        RegisterEnchantmentDefinition();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);
        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);

        Assert.True(reg.ContainsKey(ResourceLocation.Parse("betasharp:sharpness")));
    }

    [Fact]
    public void Registry_GetHolder_returns_holder_for_loaded_entry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        RegisterEnchantmentDefinition(LoadLocations.Assets);
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);
        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);

        Holder<TestEnchantment>? holder = reg.Get(ResourceLocation.Parse("betasharp:sharpness"));

        Assert.NotNull(holder);
        Assert.Equal(5, holder.Value.MaxLevel);
    }

    [Fact]
    public void Registry_enumerates_resolved_entries()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteBaseEnchantment("fortune", maxLevel: 3);

        RegisterEnchantmentDefinition(LoadLocations.Assets);
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);
        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);

        var names = reg.Select(e => e.Name).OrderBy(n => n).ToList();
        Assert.Equal(["fortune", "sharpness"], names);
    }

    // -------------------------------------------------------------------------
    // RegistryAccess — global datapack layering
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_with_datapack_path_loads_datapack_entries()
    {
        WriteDatapackEnchantment("mypack", "betasharp", "looting", maxLevel: 3);

        RegisterEnchantmentDefinition();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir, datapackPath: _tempDir);

        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);
        TestEnchantment? looting = reg.GetValue(ResourceLocation.Parse("betasharp:looting"));

        Assert.NotNull(looting);
        Assert.Equal(3, looting.MaxLevel);
    }

    [Fact]
    public void Datapack_entry_overrides_base_asset_via_merge()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5, rarity: "common");
        // Datapack bumps max level but leaves rarity alone (merge semantics).
        WriteDatapackEnchantment("mypack", "betasharp", "sharpness", maxLevel: 10);

        RegisterEnchantmentDefinition();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir, datapackPath: _tempDir);

        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);
        TestEnchantment? sharpness = reg.GetValue(ResourceLocation.Parse("betasharp:sharpness"));

        Assert.NotNull(sharpness);
        Assert.Equal(10, sharpness.MaxLevel);
        // Rarity should survive the merge since the datapack did not specify it.
        Assert.Equal("common", sharpness.Rarity);
    }

    // -------------------------------------------------------------------------
    // RegistryAccess.WithWorldDatapacks / WithoutWorldDatapacks
    // -------------------------------------------------------------------------

    [Fact]
    public void WithWorldDatapacks_adds_world_only_entries_to_active_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("worldpack", "betasharp", "mending", maxLevel: 1);

        RegisterEnchantmentDefinition();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess worldRa = serverRa.WithWorldDatapacks(Path.Combine(_tempDir, "world"));

        IReadableRegistry<TestEnchantment> worldReg = worldRa.GetOrThrow(s_enchKey);
        Assert.NotNull(worldReg.Get(ResourceLocation.Parse("betasharp:mending")));
    }

    [Fact]
    public void WithWorldDatapacks_does_not_pollute_server_level_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("worldpack", "betasharp", "mending", maxLevel: 1);

        RegisterEnchantmentDefinition();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        _ = serverRa.WithWorldDatapacks(Path.Combine(_tempDir, "world"));

        // The original serverRa must not contain the world-only enchantment.
        IReadableRegistry<TestEnchantment> serverReg = serverRa.GetOrThrow(s_enchKey);
        Assert.Null(serverReg.Get(ResourceLocation.Parse("betasharp:mending")));
    }

    [Fact]
    public void WithWorldDatapacks_overrides_base_entry_in_active_registry()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("worldpack", "betasharp", "sharpness", maxLevel: 8);

        RegisterEnchantmentDefinition();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess worldRa = serverRa.WithWorldDatapacks(Path.Combine(_tempDir, "world"));

        IReadableRegistry<TestEnchantment> worldReg = worldRa.GetOrThrow(s_enchKey);
        Assert.Equal(8, worldReg.GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);

        // Server-level is unchanged.
        IReadableRegistry<TestEnchantment> serverReg = serverRa.GetOrThrow(s_enchKey);
        Assert.Equal(5, serverReg.GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
    }

    [Fact]
    public void WithoutWorldDatapacks_returns_server_level_state_without_disk_io()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("worldpack", "betasharp", "mending", maxLevel: 1);

        RegisterEnchantmentDefinition();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess worldRa = serverRa.WithWorldDatapacks(Path.Combine(_tempDir, "world"));

        // Remove the world dir entirely to prove WithoutWorldDatapacks does no disk I/O.
        Directory.Delete(Path.Combine(_tempDir, "world"), recursive: true);

        RegistryAccess strippedRa = worldRa.WithoutWorldDatapacks();

        IReadableRegistry<TestEnchantment> reg = strippedRa.GetOrThrow(s_enchKey);
        Assert.NotNull(reg.Get(ResourceLocation.Parse("betasharp:sharpness")));
        Assert.Null(reg.Get(ResourceLocation.Parse("betasharp:mending")));
    }

    [Fact]
    public void Multiple_WithWorldDatapacks_calls_are_independent_clones()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        string worldADir = Path.Combine(_tempDir, "worldA");
        string worldBDir = Path.Combine(_tempDir, "worldB");
        string packDataA = Path.Combine(worldADir, "datapacks", "pack", "data", "betasharp", "enchantment");
        string packDataB = Path.Combine(worldBDir, "datapacks", "pack", "data", "betasharp", "enchantment");
        Directory.CreateDirectory(packDataA);
        Directory.CreateDirectory(packDataB);
        File.WriteAllText(Path.Combine(packDataA, "mending.json"), "{\"MaxLevel\":1}");
        File.WriteAllText(Path.Combine(packDataB, "looting.json"), "{\"MaxLevel\":3}");

        RegisterEnchantmentDefinition();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess raA = serverRa.WithWorldDatapacks(worldADir);
        RegistryAccess raB = serverRa.WithWorldDatapacks(worldBDir);

        IReadableRegistry<TestEnchantment> regA = raA.GetOrThrow(s_enchKey);
        IReadableRegistry<TestEnchantment> regB = raB.GetOrThrow(s_enchKey);

        Assert.NotNull(regA.Get(ResourceLocation.Parse("betasharp:mending")));
        Assert.Null(regA.Get(ResourceLocation.Parse("betasharp:looting")));

        Assert.NotNull(regB.Get(ResourceLocation.Parse("betasharp:looting")));
        Assert.Null(regB.Get(ResourceLocation.Parse("betasharp:mending")));
    }

    // -------------------------------------------------------------------------
    // Reload — simulates the /reload pipeline: Build() → [WithWorldDatapacks()] → repeat
    // -------------------------------------------------------------------------

    [Fact]
    public void Rebuild_picks_up_new_file_added_after_first_build()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        RegisterEnchantmentDefinition();
        RegistryAccess first = RegistryAccess.Build(basePath: _tempDir);
        Assert.Null(first.GetOrThrow(s_enchKey).Get(ResourceLocation.Parse("betasharp:fortune")));

        // Simulate a datapack author dropping a new file while the server is running.
        WriteBaseEnchantment("fortune", maxLevel: 3);

        RegistryAccess reloaded = RegistryAccess.Build(basePath: _tempDir);
        Assert.NotNull(reloaded.GetOrThrow(s_enchKey).Get(ResourceLocation.Parse("betasharp:fortune")));
    }

    [Fact]
    public void Rebuild_picks_up_changed_value_in_existing_file()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        RegisterEnchantmentDefinition();
        RegistryAccess first = RegistryAccess.Build(basePath: _tempDir);
        Assert.Equal(5, first.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);

        // Author edits the file to bump max level.
        WriteBaseEnchantment("sharpness", maxLevel: 10);

        RegistryAccess reloaded = RegistryAccess.Build(basePath: _tempDir);
        Assert.Equal(10, reloaded.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
    }

    [Fact]
    public void Rebuild_produces_independent_instance_old_instance_unaffected()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);

        RegisterEnchantmentDefinition();
        RegistryAccess first = RegistryAccess.Build(basePath: _tempDir);

        WriteBaseEnchantment("sharpness", maxLevel: 10);
        RegistryAccess reloaded = RegistryAccess.Build(basePath: _tempDir);

        // Callers holding a reference to the old instance must not see the new value.
        Assert.Equal(5, first.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
        Assert.Equal(10, reloaded.GetOrThrow(s_enchKey).GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
    }

    [Fact]
    public void Rebuild_then_reapply_world_datapacks_reflects_changes_in_both_tiers()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("worldpack", "betasharp", "mending", maxLevel: 1);
        string worldDir = Path.Combine(_tempDir, "world");

        RegisterEnchantmentDefinition();
        RegistryAccess serverV1 = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess worldV1 = serverV1.WithWorldDatapacks(worldDir);

        // Both tiers change between reloads.
        WriteBaseEnchantment("sharpness", maxLevel: 10);
        WriteWorldEnchantment("worldpack", "betasharp", "mending", maxLevel: 4);

        RegistryAccess serverV2 = RegistryAccess.Build(basePath: _tempDir);
        RegistryAccess worldV2 = serverV2.WithWorldDatapacks(worldDir);

        IReadableRegistry<TestEnchantment> reg = worldV2.GetOrThrow(s_enchKey);
        Assert.Equal(10, reg.GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
        Assert.Equal(4, reg.GetValue(ResourceLocation.Parse("betasharp:mending"))!.MaxLevel);

        // Old world instance is unaffected.
        IReadableRegistry<TestEnchantment> oldReg = worldV1.GetOrThrow(s_enchKey);
        Assert.Equal(5, oldReg.GetValue(ResourceLocation.Parse("betasharp:sharpness"))!.MaxLevel);
        Assert.Equal(1, oldReg.GetValue(ResourceLocation.Parse("betasharp:mending"))!.MaxLevel);
    }

    [Fact]
    public void Rebuild_without_world_does_not_contain_previous_world_entries()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteWorldEnchantment("worldpack", "betasharp", "mending", maxLevel: 1);
        string worldDir = Path.Combine(_tempDir, "world");

        RegisterEnchantmentDefinition();
        RegistryAccess serverRa = RegistryAccess.Build(basePath: _tempDir);
        _ = serverRa.WithWorldDatapacks(worldDir);

        // Reload without re-applying the world layer — simulates a server reload
        // that happens to run before the world is re-attached.
        RegistryAccess reloaded = RegistryAccess.Build(basePath: _tempDir);

        Assert.Null(reloaded.GetOrThrow(s_enchKey).Get(ResourceLocation.Parse("betasharp:mending")));
    }

    [Fact]
    public void Datapack_removed_between_reloads_is_absent_after_rebuild()
    {
        WriteBaseEnchantment("sharpness", maxLevel: 5);
        WriteDatapackEnchantment("myaddon", "betasharp", "looting", maxLevel: 3);

        RegisterEnchantmentDefinition();
        RegistryAccess first = RegistryAccess.Build(basePath: _tempDir, datapackPath: _tempDir);
        Assert.NotNull(first.GetOrThrow(s_enchKey).Get(ResourceLocation.Parse("betasharp:looting")));

        // Operator removes the datapack between reloads.
        Directory.Delete(Path.Combine(_tempDir, "datapacks", "myaddon"), recursive: true);

        RegistryAccess reloaded = RegistryAccess.Build(basePath: _tempDir, datapackPath: _tempDir);
        Assert.Null(reloaded.GetOrThrow(s_enchKey).Get(ResourceLocation.Parse("betasharp:looting")));
    }

    // -------------------------------------------------------------------------
    // GetOrThrow
    // -------------------------------------------------------------------------

    [Fact]
    public void GetOrThrow_throws_for_registry_not_added_with_AddDynamic()
    {
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        Assert.Throws<InvalidOperationException>(() => ra.GetOrThrow(s_enchKey));
    }

    [Fact]
    public void GetOrThrow_returns_registry_when_definition_was_registered()
    {
        RegisterEnchantmentDefinition();
        RegistryAccess ra = RegistryAccess.Build(basePath: _tempDir);

        IReadableRegistry<TestEnchantment> reg = ra.GetOrThrow(s_enchKey);
        Assert.NotNull(reg);
    }
}
