using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class GrassPatchFeature : Feature
{
    private readonly int _tallGrassBlockId;
    private readonly int _tallGrassBlockMeta;

    public GrassPatchFeature(int tallGrassBlockId, int tallGrassBlockMeta)
    {
        _tallGrassBlockId = tallGrassBlockId;
        _tallGrassBlockMeta = tallGrassBlockMeta;
    }

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        while (true)
        {
            int blockId = level.BlocksReader.GetBlockId(x, y, z);
            if ((blockId != 0 && blockId != Block.Leaves.id) || y <= 0)
            {
                for (int i = 0; i < 128; ++i)
                {
                    int genX = x + rand.NextInt(8) - rand.NextInt(8);
                    int genY = y + rand.NextInt(4) - rand.NextInt(4);
                    int genZ = z + rand.NextInt(8) - rand.NextInt(8);
                    if (level.BlocksReader.IsAir(genX, genY, genZ) &&
                        ((BlockPlant)Block.Blocks[_tallGrassBlockId]).canGrow(new OnTickEvt(level, genX, genY, genZ, level.BlocksReader.GetMeta(genX, genY, genZ), level.BlocksReader.GetBlockId(genX, genY, genZ))))
                    {
                        level.BlockWriter.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, _tallGrassBlockId, _tallGrassBlockMeta, notifyBlockPlaced: false);
                    }
                }

                return true;
            }

            --y;
        }
    }
}
