using BetaSharp.Registries;
using BetaSharp.Registries.Data;

namespace BetaSharp.Tests;

public class RegistryKeyTests
{
    // ---- RegistryKey<T> ----

    [Fact]
    public void RegistryKey_exposes_location()
    {
        var loc = ResourceLocation.Parse("betasharp:game_mode");
        var key = new RegistryKey<GameMode>(loc);

        Assert.Equal(loc, key.Location);
    }

    [Fact]
    public void RegistryKey_ToString_returns_location_string()
    {
        var key = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));

        Assert.Equal("betasharp:game_mode", key.ToString());
    }

    [Fact]
    public void RegistryKey_equals_another_with_same_location_and_type()
    {
        var k1 = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));
        var k2 = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));

        Assert.Equal(k1, k2);
        Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
    }

    [Fact]
    public void RegistryKey_not_equal_for_different_location()
    {
        var k1 = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));
        var k2 = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:enchantment"));

        Assert.NotEqual(k1, k2);
    }

    // ---- RegistryDefinition<T> ----

    [Fact]
    public void RegistryDefinition_exposes_key_and_asset_path()
    {
        var key = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));
        var def = new RegistryDefinition<GameMode>(key, "gamemode");

        Assert.Equal(key, def.Key);
        Assert.Equal("gamemode", def.AssetPath);
    }

    [Fact]
    public void RegistryDefinition_creates_loader_with_alldata_locations_by_default()
    {
        var key = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));
        var def = new RegistryDefinition<GameMode>(key, "gamemode");

        DataAssetLoader<GameMode> loader = def.CreateLoader();

        // Loader should load from all data locations (Assets | GameDatapack | WorldDatapack)
        Assert.NotNull(loader);
    }

    [Fact]
    public void RegistryDefinition_respects_explicit_locations()
    {
        var key = new RegistryKey<GameMode>(ResourceLocation.Parse("betasharp:game_mode"));
        var def = new RegistryDefinition<GameMode>(key, "gamemode", LoadLocations.Assets);

        DataAssetLoader<GameMode> loader = def.CreateLoader();

        Assert.NotNull(loader);
    }

    // ---- Well-known RegistryKeys constants ----

    [Fact]
    public void RegistryKeys_GameModes_has_expected_location()
    {
        Assert.Equal("betasharp:game_mode", RegistryKeys.GameModes.ToString());
    }

    [Fact]
    public void RegistryKeys_GameModesDefinition_matches_GameModes_key()
    {
        Assert.Equal(RegistryKeys.GameModes, RegistryDefinitions.GameModes.Key);
    }
}
