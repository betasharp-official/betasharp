using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class PlantPatchFeature : Feature
{

    private int plantBlockId;

    public PlantPatchFeature(int plantBlockId)
    {
        this.plantBlockId = plantBlockId;
    }

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (world.isAir(genX, genY, genZ) && ((BlockPlant)Block.Blocks[plantBlockId]).canGrow(world, genX, genY, genZ))
            {
                world.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, plantBlockId);
            }
        }

        return true;
    }
}
