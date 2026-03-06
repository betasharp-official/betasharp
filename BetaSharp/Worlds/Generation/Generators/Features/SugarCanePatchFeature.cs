using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SugarCanePatchFeature : Feature
{
    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 20; ++i)
        {
            int genX = x + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(4) - rand.NextInt(4);

            if (!world.IsAir(genX, y, genZ))
            {
                continue;
            }

            bool hasWaterNearby = world.GetMaterial(genX - 1, y - 1, genZ) == Material.Water ||
                                  world.GetMaterial(genX + 1, y - 1, genZ) == Material.Water ||
                                  world.GetMaterial(genX, y - 1, genZ - 1) == Material.Water ||
                                  world.GetMaterial(genX, y - 1, genZ + 1) == Material.Water;

            if (hasWaterNearby)
            {
                int height = 2 + rand.NextInt(rand.NextInt(3) + 1);

                for (int h = 0; h < height; ++h)
                {
                    if (Block.SugarCane.canGrow(world, genX, y + h, genZ))
                    {
                        world.SetBlockWithoutNotifyingNeighbors(genX, y + h, genZ, Block.SugarCane.id);
                    }
                }
            }
        }

        return true;
    }
}
