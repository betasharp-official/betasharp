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
        if (world.GetBlockId(x, y + 1, z) != Block.Netherrack.id)
        {
            return false;
        }

        if (world.GetBlockId(x, y, z) != 0 && world.GetBlockId(x, y, z) != Block.Netherrack.id)
        {
            return false;
        }

        int netherrackNeighbors = 0;
        if (world.GetBlockId(x - 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.GetBlockId(x + 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.GetBlockId(x, y, z - 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.GetBlockId(x, y, z + 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (world.GetBlockId(x, y - 1, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
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

        if (world.IsAir(x, y - 1, z))
        {
            ++airNeighbors;
        }

        if (netherrackNeighbors == 4 && airNeighbors == 1)
        {
            world.SetBlock(x, y, z, _lavaBlockId);

            world.InstantBlockUpdateEnabled = true;
            Block.Blocks[_lavaBlockId].onTick(world, x, y, z, rand);
            world.InstantBlockUpdateEnabled = false;
        }

        return true;
    }
}
