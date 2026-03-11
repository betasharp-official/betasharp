namespace BetaSharp.Worlds;

public class WorldSettings
{
    public long Seed { get; }
    public WorldType TerrainType { get; }

    public WorldSettings(long seed, WorldType terrainType)
    {
        Seed = seed;
        TerrainType = terrainType;
    }
}
