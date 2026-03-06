using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class GlowstoneClusterFeature : Feature
{
    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        if (!world.IsAir(x, y, z))
        {
            return false;
        }

        if (world.GetBlockId(x, y + 1, z) != Block.Netherrack.id)
        {
            return false;
        }


        world.SetBlock(x, y, z, Block.Glowstone.id);

        for (int i = 0; i < 1500; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y - rand.NextInt(12);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(genX, genY, genZ) == 0)
            {
                int GlowstoneNeighbors = 0;

                for (int j = 0; j < 6; ++j)
                {
                    int blockId = 0;
                    if (j == 0)
                    {
                        blockId = world.GetBlockId(genX - 1, genY, genZ);
                    }

                    if (j == 1)
                    {
                        blockId = world.GetBlockId(genX + 1, genY, genZ);
                    }

                    if (j == 2)
                    {
                        blockId = world.GetBlockId(genX, genY - 1, genZ);
                    }

                    if (j == 3)
                    {
                        blockId = world.GetBlockId(genX, genY + 1, genZ);
                    }

                    if (j == 4)
                    {
                        blockId = world.GetBlockId(genX, genY, genZ - 1);
                    }

                    if (j == 5)
                    {
                        blockId = world.GetBlockId(genX, genY, genZ + 1);
                    }

                    if (blockId == Block.Glowstone.id)
                    {
                        ++GlowstoneNeighbors;
                    }
                }

                if (GlowstoneNeighbors == 1)
                {
                    world.SetBlock(genX, genY, genZ, Block.Glowstone.id);
                }
            }
        }

        return true;
    }
}
