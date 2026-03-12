namespace BetaSharp.Worlds;

public class WorldSettings
{
    public long Seed { get; }
    public WorldType TerrainType { get; }
    public string GeneratorOptions { get; }

    public WorldSettings(long seed, WorldType terrainType, string generatorOptions = "")
    {
        Seed = seed;
        TerrainType = terrainType;
        GeneratorOptions = generatorOptions;
    }
}
