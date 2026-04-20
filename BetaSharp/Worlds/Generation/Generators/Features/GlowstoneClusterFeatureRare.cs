using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class GlowstoneClusterFeatureRare : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        if (!level.Reader.IsAir(x, y, z))
        {
            return false;
        }

        if (level.Reader.GetBlockId(x, y + 1, z) != Block.Netherrack.ID)
        {
            return false;
        }


        level.Writer.SetBlock(x, y, z, Block.Glowstone.ID, 0, false);

        for (int i = 0; i < 1500; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y - rand.NextInt(12);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (level.Reader.GetBlockId(genX, genY, genZ) == 0)
            {
                int GlowstoneNeighbors = 0;

                for (int j = 0; j < 6; ++j)
                {
                    int blockId = 0;
                    if (j == 0)
                    {
                        blockId = level.Reader.GetBlockId(genX - 1, genY, genZ);
                    }

                    if (j == 1)
                    {
                        blockId = level.Reader.GetBlockId(genX + 1, genY, genZ);
                    }

                    if (j == 2)
                    {
                        blockId = level.Reader.GetBlockId(genX, genY - 1, genZ);
                    }

                    if (j == 3)
                    {
                        blockId = level.Reader.GetBlockId(genX, genY + 1, genZ);
                    }

                    if (j == 4)
                    {
                        blockId = level.Reader.GetBlockId(genX, genY, genZ - 1);
                    }

                    if (j == 5)
                    {
                        blockId = level.Reader.GetBlockId(genX, genY, genZ + 1);
                    }

                    if (blockId == Block.Glowstone.ID)
                    {
                        ++GlowstoneNeighbors;
                    }
                }

                if (GlowstoneNeighbors == 1)
                {
                    level.Writer.SetBlock(genX, genY, genZ, Block.Glowstone.ID, 0, false);
                }
            }
        }

        return true;
    }
}
