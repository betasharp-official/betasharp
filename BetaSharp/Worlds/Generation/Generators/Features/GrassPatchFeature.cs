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

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        while (true)
        {
            int blockId = world.GetBlockId(x, y, z);
            if ((blockId != 0 && blockId != Block.Leaves.id) || y <= 0)
            {
                for (int i = 0; i < 128; ++i)
                {
                    int genX = x + rand.NextInt(8) - rand.NextInt(8);
                    int genY = y + rand.NextInt(4) - rand.NextInt(4);
                    int genZ = z + rand.NextInt(8) - rand.NextInt(8);
                    if (world.IsAir(genX, genY, genZ) && ((BlockPlant)Block.Blocks[_tallGrassBlockId]).canGrow(world, genX, genY, genZ))
                    {
                        world.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, _tallGrassBlockId, _tallGrassBlockMeta);
                    }
                }

                return true;
            }

            --y;
        }
    }
}
