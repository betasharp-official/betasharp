using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class CactusPatchFeature : Feature
{
    public override bool Generate(IWorldContext level, int x, int y, int z)
    {
        for (int i = 0; i < 10; ++i)
        {
            int genX = x + level.random.NextInt(8) - level.random.NextInt(8);
            int genY = y + level.random.NextInt(4) - level.random.NextInt(4);
            int genZ = z + level.random.NextInt(8) - level.random.NextInt(8);
            if (level.BlocksReader.IsAir(genX, genY, genZ))
            {
                int height = 1 + level.random.NextInt(level.random.NextInt(3) + 1);

                for (int h = 0; h < height; ++h)
                {
                    if (Block.Cactus.canGrow(new OnTickEvt(level, genX, genY + h, genZ, level.BlocksReader.GetMeta(genX, genY + h, genZ), level.BlocksReader.GetBlockId(genX, genY + h, genZ))))
                    {
                        level.BlockWriter.SetBlockWithoutNotifyingNeighbors(genX, genY + h, genZ, Block.Cactus.id, 0, notifyBlockPlaced: false);
                    }
                }
            }
        }

        return true;
    }
}
