using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class CactusPatchFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 10; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (level.BlocksReader.IsAir(genX, genY, genZ))
            {
                int height = 1 + rand.NextInt(rand.NextInt(3) + 1);

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
