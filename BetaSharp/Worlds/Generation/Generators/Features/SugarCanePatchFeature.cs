using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SugarCanePatchFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 20; ++i)
        {
            int genX = x + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(4) - rand.NextInt(4);

            if (!level.Reader.IsAir(genX, y, genZ))
            {
                continue;
            }

            bool hasWaterNearby = level.Reader.GetMaterial(genX - 1, y - 1, genZ) == Material.Water ||
                                  level.Reader.GetMaterial(genX + 1, y - 1, genZ) == Material.Water ||
                                  level.Reader.GetMaterial(genX, y - 1, genZ - 1) == Material.Water ||
                                  level.Reader.GetMaterial(genX, y - 1, genZ + 1) == Material.Water;

            if (hasWaterNearby)
            {
                int height = 2 + rand.NextInt(rand.NextInt(3) + 1);

                for (int h = 0; h < height; ++h)
                {
                    if (Block.SugarCane.CanGrow(new OnTickEvent(level, genX, y + h, genZ, level.Reader.GetBlockMeta(genX, y + h, genZ), level.Reader.GetBlockId(genX, y + h, genZ))))
                    {
                        level.Writer.SetBlock(genX, y + h, genZ, Block.SugarCane.ID, 0, false);
                    }
                }
            }
        }

        return true;
    }
}
