using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SugarCanePatchFeature : Feature
{
    public override bool Generate(IWorldContext level, int x, int y, int z)
    {
        for (int i = 0; i < 20; ++i)
        {
            int genX = x + level.random.NextInt(4) - level.random.NextInt(4);
            int genZ = z + level.random.NextInt(4) - level.random.NextInt(4);

            if (!level.BlocksReader.IsAir(genX, y, genZ))
            {
                continue;
            }

            bool hasWaterNearby = level.BlocksReader.GetMaterial(genX - 1, y - 1, genZ) == Material.Water ||
                                  level.BlocksReader.GetMaterial(genX + 1, y - 1, genZ) == Material.Water ||
                                  level.BlocksReader.GetMaterial(genX, y - 1, genZ - 1) == Material.Water ||
                                  level.BlocksReader.GetMaterial(genX, y - 1, genZ + 1) == Material.Water;

            if (hasWaterNearby)
            {
                int height = 2 + level.random.NextInt(level.random.NextInt(3) + 1);

                for (int h = 0; h < height; ++h)
                {
                    if (Block.SugarCane.canGrow(new OnTickEvt(level, genX, y + h, genZ, level.BlocksReader.GetMeta(genX, y + h, genZ), level.BlocksReader.GetBlockId(genX, y + h, genZ))))
                    {
                        level.BlockWriter.SetBlock(genX, y + h, genZ, Block.SugarCane.id, 0, doUpdate: false);
                    }
                }
            }
        }

        return true;
    }
}
