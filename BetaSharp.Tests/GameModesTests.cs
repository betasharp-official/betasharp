using BetaSharp.DataAsset;
using BetaSharp.GameMode;
using BetaSharp.Registries;
using GameModeClass = BetaSharp.GameMode.GameMode;

namespace BetaSharp.Tests;

[Collection("RegistryAccess")]
public class GameModesTests : IDisposable
{
    private readonly string _tempDir;

    public GameModesTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);

        RegistryAccess.ClearDynamicEntries();
        RegistryAccess.AddDynamic(RegistryKeys.GameModesDefinition);
    }

    public void Dispose()
    {
        RegistryAccess.ClearDynamicEntries();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ---- Helpers ----

    private RegistryAccess BuildWithGameModes(params (string name, bool disallowFlying)[] modes)
    {
        string dir = Path.Combine(_tempDir, "assets", "gamemode");
        Directory.CreateDirectory(dir);
        foreach ((string name, bool disallowFlying) in modes)
        {
            File.WriteAllText(Path.Combine(dir, $"{name}.json"),
                $"{{\"DisallowFlying\":{disallowFlying.ToString().ToLower()}}}");
        }

        return RegistryAccess.Build(basePath: _tempDir);
    }

    // ---- GameModes.TryGet ----

    [Fact]
    public void TryGet_returns_true_and_game_mode_for_existing_name()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true), ("creative", false));

        bool found = GameModes.TryGet(ra, "survival", out GameModeClass? mode);

        Assert.True(found);
        Assert.NotNull(mode);
        Assert.Equal("survival", mode.Name);
    }

    [Fact]
    public void TryGet_returns_false_for_unknown_name()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true));

        bool found = GameModes.TryGet(ra, "adventure", out GameModeClass? mode);

        Assert.False(found);
        Assert.Null(mode);
    }

    [Fact]
    public void TryGet_short_name_matches_by_prefix()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true), ("creative", false));

        bool found = GameModes.TryGet(ra, "s", out GameModeClass? mode, shortName: true);

        Assert.True(found);
        Assert.NotNull(mode);
        Assert.StartsWith("s", mode.Name);
    }

    [Fact]
    public void TryGet_short_name_prefix_match()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true), ("creative", false));

        bool found = GameModes.TryGet(ra, "surv", out GameModeClass? mode, shortName: true);

        Assert.True(found);
        Assert.Equal("survival", mode!.Name);
    }

    // ---- GameModes.Get ----

    [Fact]
    public void Get_returns_game_mode_for_existing_name()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true));

        GameModeClass mode = GameModes.Get(ra, "survival");

        Assert.Equal("survival", mode.Name);
    }

    [Fact]
    public void Get_throws_for_unknown_name()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true));

        Assert.Throws<ArgumentException>(() => GameModes.Get(ra, "adventure"));
    }

    // ---- GameModes.SetDefaultGameMode ----

    [Fact]
    public void SetDefaultGameMode_with_name_sets_default_to_named_mode()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true), ("creative", false));

        GameModes.SetDefaultGameMode(ra, "creative");

        Assert.Equal("creative", GameModes.DefaultGameMode.Name);
        Assert.False(GameModes.DefaultGameMode.DisallowFlying);
    }

    [Fact]
    public void SetDefaultGameMode_without_name_prefers_survival()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true), ("creative", false));

        GameModes.SetDefaultGameMode(ra);

        Assert.Equal("survival", GameModes.DefaultGameMode.Name);
    }

    [Fact]
    public void SetDefaultGameMode_without_name_falls_back_to_default_when_no_survival()
    {
        RegistryAccess ra = BuildWithGameModes(("default", false), ("adventure", true));

        GameModes.SetDefaultGameMode(ra);

        Assert.Equal("default", GameModes.DefaultGameMode.Name);
    }

    [Fact]
    public void SetDefaultGameMode_without_name_falls_back_to_first_when_no_survival_or_default()
    {
        RegistryAccess ra = BuildWithGameModes(("adventure", true));

        GameModes.SetDefaultGameMode(ra);

        Assert.Equal("adventure", GameModes.DefaultGameMode.Name);
    }

    [Fact]
    public void SetDefaultGameMode_with_empty_name_falls_back_to_survival()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true), ("creative", false));

        GameModes.SetDefaultGameMode(ra, "");

        Assert.Equal("survival", GameModes.DefaultGameMode.Name);
    }

    // ---- JSON property loading ----

    [Fact]
    public void Game_mode_properties_are_loaded_from_json()
    {
        RegistryAccess ra = BuildWithGameModes(("creative", false));

        GameModeClass creative = GameModes.Get(ra, "creative");

        Assert.False(creative.DisallowFlying);
        // Unspecified properties retain their default values.
        Assert.True(creative.CanBreak);
    }

    // ---- Namespace is set on loaded entries ----

    [Fact]
    public void Loaded_game_mode_has_betasharp_namespace()
    {
        RegistryAccess ra = BuildWithGameModes(("survival", true));

        GameModeClass survival = GameModes.Get(ra, "survival");

        Assert.Equal(Namespace.BetaSharp, survival.Namespace);
    }
}
