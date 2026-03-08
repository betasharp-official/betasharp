using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class NetherLavaSpringFeature : Feature
{
    private readonly int _lavaBlockId;

    public NetherLavaSpringFeature(int lavaBlockId) => _lavaBlockId = lavaBlockId;

    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        if (world.getBlockId(x, y + 1, z) != Block.Netherrack.id)
        {
            return false;
        }

        if (world.getBlockId(x, y, z) != 0 && world.getBlockId(x, y, z) != Block.Netherrack.id)
        {
            return false;
        }

        int netherrackNeighbors = 0;
        if (world.getBlockId(x - 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.getBlockId(x + 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.getBlockId(x, y, z - 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.getBlockId(x, y, z + 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.getBlockId(x, y - 1, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }


        int airNeighbors = 0;
        if (world.isAir(x - 1, y, z))
        {
            ++airNeighbors;
        }

        if (world.isAir(x + 1, y, z))
        {
            ++airNeighbors;
        }

        if (world.isAir(x, y, z - 1))
        {
            ++airNeighbors;
        }

        if (world.isAir(x, y, z + 1))
        {
            ++airNeighbors;
        }

        if (world.isAir(x, y - 1, z))
        {
            ++airNeighbors;
        }

        if (netherrackNeighbors == 4 && airNeighbors == 1)
        {
            world.setBlock(x, y, z, _lavaBlockId);

            world.InstantBlockUpdateEnabled = true;
            Block.Blocks[_lavaBlockId].onTick(world.BlocksView, x, y, z, rand, world.WorldEventBroadcaster, world.isRemote);
            world.InstantBlockUpdateEnabled = false;
        }

        return true;
    }
}
