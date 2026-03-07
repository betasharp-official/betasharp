using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class PumpkinPatchFeature : Feature
{

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (world.isAir(genX, genY, genZ) &&
                world.getBlockId(genX, genY - 1, genZ) == Block.GrassBlock.id &&
                Block.Pumpkin.canPlaceAt(world, genX, genY, genZ))
            {
                world.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, Block.Pumpkin.id, rand.NextInt(4));
            }
        }

        return true;
    }
}
