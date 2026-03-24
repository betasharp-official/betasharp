namespace BetaSharp.Worlds.Core.Systems;

public class WorldSettings
{
    public WorldSettings(long seed, WorldType terrainType, string generatorOptions = "")
    {
        Seed = seed;
        TerrainType = terrainType;
        GeneratorOptions = generatorOptions;
    }

    public long Seed { get; }
    public WorldType TerrainType { get; }
    public string GeneratorOptions { get; }
}
