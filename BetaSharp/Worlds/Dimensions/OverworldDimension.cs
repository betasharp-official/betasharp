using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Chunks;
using BetaSharp.Worlds.Gen.Flat;
using BetaSharp.Worlds.Generation.Biomes;
using BetaSharp.Worlds.Generation.Biomes.Source;

namespace BetaSharp.Worlds.Dimensions;

internal class OverworldDimension : Dimension
{
    public override void InitBiomeSource()
    {
        if (World.Properties.TerrainType == WorldType.Sky)
        {
            BiomeSource = new FixedBiomeSource(Biome.Sky, 0.5D, 0.0D);
            return;
        }
        base.InitBiomeSource();
    }

    public override IChunkSource CreateChunkGenerator()
    {
        WorldType terrainType = World.Properties.TerrainType;

        if (terrainType == WorldType.Flat)
        {
            return new FlatChunkGenerator(World);
        }

        if (terrainType == WorldType.Sky)
        {
            return new SkyChunkGenerator(World, World.Seed);
        }

        return base.CreateChunkGenerator();
    }

    public override bool IsValidSpawnPoint(int x, int z)
    {
        if (World.Properties.TerrainType == WorldType.Flat)
        {
            return true;
        }

        if (World.Properties.TerrainType == WorldType.Sky)
        {
            int topSolidY = World.Reader.GetTopSolidBlockY(x, z);
            if (topSolidY <= 0) return false;
            int blockId = World.Reader.GetBlockId(x, topSolidY - 1, z);
            return blockId != 0 && Block.Blocks[blockId] != null && Block.Blocks[blockId].Material.BlocksMovement;
        }

        return base.IsValidSpawnPoint(x, z);
    }

    public override float GetTimeOfDay(long time, float partialTicks)
    {
        if (World.Properties.TerrainType == WorldType.Sky)
        {
            return 0.0F;
        }
        return base.GetTimeOfDay(time, partialTicks);
    }

    public override float CloudHeight => World.Properties.TerrainType == WorldType.Sky ? 8.0F : base.CloudHeight;
}
