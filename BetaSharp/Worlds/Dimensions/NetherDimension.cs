using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Gen.Chunks;
using BetaSharp.Worlds.Generation.Biomes;
using BetaSharp.Worlds.Generation.Biomes.Source;
using Silk.NET.Maths;

namespace BetaSharp.Worlds.Dimensions;

internal class NetherDimension : Dimension
{
    public override void InitBiomeSource()
    {
        BiomeSource = new FixedBiomeSource(Biome.Hell, 1.0D, 0.0D);
        IsNether = true;
        EvaporatesWater = true;
        HasCeiling = true;
        Id = -1;
    }

    public override bool HasWorldSpawn => false;

    public override Vector3D<double> GetFogColor(float celestialAngle, float partialTicks)
    {
        return new Vector3D<double>(0.2, 0.03, 0.03);
    }

    protected override void InitBrightnessTable()
    {
        float offset = 0.1F;

        for (int i = 0; i <= 15; ++i)
        {
            float factor = 1.0F - i / 15.0F;
            LightLevelToLuminance[i] = (1.0F - factor) / (factor * 3.0F + 1.0F) * (1.0F - offset) + offset;
        }
    }

    public override IChunkSource CreateChunkGenerator()
    {
        return new NetherChunkGenerator(World, World.Seed);
    }

    public override bool IsValidSpawnPoint(int x, int z)
    {
        int blockId = World.GetSpawnBlockId(x, z);
        return blockId != Block.Bedrock.ID && blockId != 0 && Block.BlocksOpaque[blockId];
    }

    public override float GetTimeOfDay(long time, float tickDelta) => 0.5F;
}
