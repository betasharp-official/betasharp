using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class GlowstoneClusterFeature : Feature
{
    public override bool Generate(IBlockWorldContext level, int x, int y, int z)
    {
        if (!level.BlocksReader.IsAir(x, y, z))
        {
            return false;
        }

        if (level.BlocksReader.GetBlockId(x, y + 1, z) != Block.Netherrack.id)
        {
            return false;
        }


        level.BlockWriter.SetBlock(x, y, z, Block.Glowstone.id);

        for (int i = 0; i < 1500; ++i)
        {
            int genX = x + level.random.NextInt(8) - level.random.NextInt(8);
            int genY = y - level.random.NextInt(12);
            int genZ = z + level.random.NextInt(8) - level.random.NextInt(8);

            if (level.BlocksReader.GetBlockId(genX, genY, genZ) == 0)
            {
                int GlowstoneNeighbors = 0;

                for (int j = 0; j < 6; ++j)
                {
                    int blockId = 0;
                    if (j == 0)
                    {
                        blockId = level.BlocksReader.GetBlockId(genX - 1, genY, genZ);
                    }

                    if (j == 1)
                    {
                        blockId = level.BlocksReader.GetBlockId(genX + 1, genY, genZ);
                    }

                    if (j == 2)
                    {
                        blockId = level.BlocksReader.GetBlockId(genX, genY - 1, genZ);
                    }

                    if (j == 3)
                    {
                        blockId = level.BlocksReader.GetBlockId(genX, genY + 1, genZ);
                    }

                    if (j == 4)
                    {
                        blockId = level.BlocksReader.GetBlockId(genX, genY, genZ - 1);
                    }

                    if (j == 5)
                    {
                        blockId = level.BlocksReader.GetBlockId(genX, genY, genZ + 1);
                    }

                    if (blockId == Block.Glowstone.id)
                    {
                        ++GlowstoneNeighbors;
                    }
                }

                if (GlowstoneNeighbors == 1)
                {
                    level.BlockWriter.SetBlock(genX, genY, genZ, Block.Glowstone.id);
                }
            }
        }

        return true;
    }
}
