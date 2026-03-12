using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Flat;

namespace BetaSharp.Worlds.Dimensions;

internal class OverworldDimension : Dimension
{
    public override ChunkSource CreateChunkGenerator()
    {
        if (World.getProperties().TerrainType == WorldType.Flat)
        {
            return new FlatChunkGenerator(World);
        }

        return base.CreateChunkGenerator();
    }

    public override bool IsValidSpawnPoint(int x, int z)
    {
        if (World.getProperties().TerrainType == WorldType.Flat)
        {
            return true;
        }
        return base.IsValidSpawnPoint(x, z);
    }
}
