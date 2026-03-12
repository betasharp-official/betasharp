using BetaSharp.Worlds.Biomes;
using BetaSharp.Worlds.Gen.Chunks;
using BetaSharp.Blocks;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds.Dimensions;

public class SkyDimension : Dimension
{
    public SkyDimension()
    {
    }

    public override void InitBiomeSource()
    {
        BiomeSource = new FixedBiomeSource(Biome.Sky, 0.5D, 0.0D);
    }

    public override ChunkSource CreateChunkGenerator()
    {
        return new SkyChunkGenerator(World, World.getSeed());
    }

    public override float GetTimeOfDay(long time, float partialTicks)
    {
        return 0.0F;
    }

    public override bool IsValidSpawnPoint(int x, int y) // Variable named y here but is actually z in Minecraft coords
    {
        int topBlockId = World.getSpawnBlockId(x, y);
        return topBlockId != 0 && Block.Blocks[topBlockId] != null && Block.Blocks[topBlockId].material.BlocksMovement;
    }

    public override float CloudHeight => 8.0F;
}
