using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Chunks;
using BetaSharp.Worlds.Gen.Flat;
using BetaSharp.Worlds.Generation.Generators.Chunks;

namespace BetaSharp.Worlds.Dimensions;

internal class OverworldDimension : Dimension
{
    public override IChunkSource CreateChunkGenerator()
    {
        WorldType terrainType = World.Properties.TerrainType;

        if (terrainType == WorldType.Flat)
        {
            return new FlatChunkGenerator(World);
        }

        if (terrainType == WorldType.Sky)
        {
            return new SkyIChunkGenerator(World, World.Seed);
        }

        return base.CreateChunkGenerator();
    }

    public override bool IsValidSpawnPoint(int x, int z)
    {
        if (World.Properties.TerrainType == WorldType.Flat)
        {
            return true;
        }

        return base.IsValidSpawnPoint(x, z);
    }
}
