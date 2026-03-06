using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SpringFeature : Feature
{
    private readonly int _liquidBlockId;

    public SpringFeature(int liquidBlockId) => _liquidBlockId = liquidBlockId;

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        if (world.GetBlockId(x, y + 1, z) != Block.Stone.id)
        {
            return false;
        }

        if (world.GetBlockId(x, y - 1, z) != Block.Stone.id)
        {
            return false;
        }

        int targetId = world.GetBlockId(x, y, z);
        if (targetId != 0 && targetId != Block.Stone.id)
        {
            return false;
        }

        int stoneNeighbors = 0;
        if (world.GetBlockId(x - 1, y, z) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (world.GetBlockId(x + 1, y, z) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (world.GetBlockId(x, y, z - 1) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (world.GetBlockId(x, y, z + 1) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }


        int airNeighbors = 0;
        if (world.IsAir(x - 1, y, z))
        {
            ++airNeighbors;
        }

        if (world.IsAir(x + 1, y, z))
        {
            ++airNeighbors;
        }

        if (world.IsAir(x, y, z - 1))
        {
            ++airNeighbors;
        }

        if (world.IsAir(x, y, z + 1))
        {
            ++airNeighbors;
        }


        if (stoneNeighbors == 3 && airNeighbors == 1)
        {
            world.SetBlock(x, y, z, _liquidBlockId);

            world.InstantBlockUpdateEnabled = true;
            Block.Blocks[_liquidBlockId].onTick(world, x, y, z, rand);
            world.InstantBlockUpdateEnabled = false;
        }

        return true;
    }
}
