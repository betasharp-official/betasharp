using BetaSharp.Worlds.Gen.Chunks;
using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Generation.Biomes.Source;
using BetaSharp.Worlds.Generation.Biomes;

namespace BetaSharp.Worlds.Dimensions;

public class SkyDimension : Dimension
{

    public override void InitBiomeSource()
    {
        BiomeSource = new FixedBiomeSource(Biome.Sky, 0.5D, 0.0D);
    }

    public override IChunkSource CreateChunkGenerator()
    {
        return new SkyChunkGenerator(World, World.Seed);
    }

    public override float GetTimeOfDay(long time, float partialTicks)
    {
        return 0.0F;
    }

    public override bool IsValidSpawnPoint(int x, int y)
    {
        int topBlockId = World.GetSpawnBlockId(x, y);
        return topBlockId != 0 && Block.Blocks[topBlockId] != null && Block.Blocks[topBlockId].material.BlocksMovement;
    }

    public override float CloudHeight => 8.0F;
}
