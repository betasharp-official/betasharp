using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

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
            int blockId = level.Reader.GetBlockId(x, y, z);
            if ((blockId != 0 && blockId != Block.Leaves.ID) || y <= 0)
            {
                for (int i = 0; i < 128; ++i)
                {
                    int genX = x + rand.NextInt(8) - rand.NextInt(8);
                    int genY = y + rand.NextInt(4) - rand.NextInt(4);
                    int genZ = z + rand.NextInt(8) - rand.NextInt(8);
                    if (level.Reader.IsAir(genX, genY, genZ) &&
                        ((BlockPlant)Block.Blocks[_tallGrassBlockId]).CanGrow(new OnTickEvent(level, genX, genY, genZ, level.Reader.GetBlockMeta(genX, genY, genZ), level.Reader.GetBlockId(genX, genY, genZ))))
                    {
                        level.Writer.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, _tallGrassBlockId, _tallGrassBlockMeta, false);
                    }
                }

                return true;
            }

            --y;
        }
    }
}
