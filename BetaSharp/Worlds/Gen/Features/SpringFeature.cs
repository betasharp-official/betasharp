using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Gen.Features;

internal class SpringFeature : Feature
{

    private readonly int _liquidBlockId;

    public SpringFeature(int liquidBlockId)
    {
        _liquidBlockId = liquidBlockId;
    }

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        if (world.getBlockId(x, y + 1, z) != Block.Stone.id) return false;

        if (world.getBlockId(x, y - 1, z) != Block.Stone.id) return false;

        int targetId = world.getBlockId(x, y, z);
        if (targetId != 0 && targetId != Block.Stone.id) return false;

        int stoneNeighbors = 0;
        if (world.getBlockId(x - 1, y, z) == Block.Stone.id) ++stoneNeighbors;
        if (world.getBlockId(x + 1, y, z) == Block.Stone.id) ++stoneNeighbors;
        if (world.getBlockId(x, y, z - 1) == Block.Stone.id) ++stoneNeighbors;
        if (world.getBlockId(x, y, z + 1) == Block.Stone.id) ++stoneNeighbors;


        int airNeighbors = 0;
        if (world.isAir(x - 1, y, z)) ++airNeighbors;
        if (world.isAir(x + 1, y, z)) ++airNeighbors;
        if (world.isAir(x, y, z - 1)) ++airNeighbors;
        if (world.isAir(x, y, z + 1)) ++airNeighbors;


        if (stoneNeighbors == 3 && airNeighbors == 1)
        {
            world.setBlock(x, y, z, _liquidBlockId);

            world.instantBlockUpdateEnabled = true;
            Block.Blocks[_liquidBlockId].onTick(world, x, y, z, rand);
            world.instantBlockUpdateEnabled = false;
        }

        return true;
    }
}
