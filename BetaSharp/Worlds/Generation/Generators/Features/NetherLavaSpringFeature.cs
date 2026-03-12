using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class NetherLavaSpringFeature : Feature
{
    private readonly int _lavaBlockId;

    public NetherLavaSpringFeature(int lavaBlockId) => _lavaBlockId = lavaBlockId;

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        if (level.BlocksReader.GetBlockId(x, y + 1, z) != Block.Netherrack.id)
        {
            return false;
        }

        if (level.BlocksReader.GetBlockId(x, y, z) != 0 && level.BlocksReader.GetBlockId(x, y, z) != Block.Netherrack.id)
        {
            return false;
        }

        int netherrackNeighbors = 0;
        if (level.BlocksReader.GetBlockId(x - 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x + 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x, y, z - 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x, y, z + 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x, y - 1, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }


        int airNeighbors = 0;
        if (level.BlocksReader.IsAir(x - 1, y, z))
        {
            ++airNeighbors;
        }

        if (level.BlocksReader.IsAir(x + 1, y, z))
        {
            ++airNeighbors;
        }

        if (level.BlocksReader.IsAir(x, y, z - 1))
        {
            ++airNeighbors;
        }

        if (level.BlocksReader.IsAir(x, y, z + 1))
        {
            ++airNeighbors;
        }

        if (level.BlocksReader.IsAir(x, y - 1, z))
        {
            ++airNeighbors;
        }

        if (netherrackNeighbors == 4 && airNeighbors == 1)
        {
            level.BlockWriter.SetBlock(x, y, z, _lavaBlockId, 0, doUpdate: false);
            level.TickScheduler.TriggerInstantTick(x, y, z, _lavaBlockId);
        }

        return true;
    }
}
