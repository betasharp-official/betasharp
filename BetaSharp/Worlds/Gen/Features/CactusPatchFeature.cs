using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class CactusPatchFeature : Feature
{

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 10; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (world.isAir(genX, genY, genZ))
            {
                int height = 1 + rand.NextInt(rand.NextInt(3) + 1);

                for (int h = 0; h < height; ++h)
                {
                    if (Block.Cactus.canGrow(world, genX, genY + h, genZ))
                    {
                        world.SetBlockWithoutNotifyingNeighbors(genX, genY + h, genZ, Block.Cactus.id);
                    }
                }
            }
        }

        return true;
    }
}
