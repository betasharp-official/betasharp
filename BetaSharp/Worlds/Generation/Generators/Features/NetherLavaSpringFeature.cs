using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class NetherLavaSpringFeature : Feature
{
    private readonly int _lavaBlockId;

    public NetherLavaSpringFeature(int lavaBlockId) => _lavaBlockId = lavaBlockId;

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        if (level.Reader.GetBlockId(x, y + 1, z) != Block.Netherrack.id)
        {
            return false;
        }

        if (level.Reader.GetBlockId(x, y, z) != 0 && level.Reader.GetBlockId(x, y, z) != Block.Netherrack.id)
        {
            return false;
        }

        int netherrackNeighbors = 0;
        if (level.Reader.GetBlockId(x - 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.Reader.GetBlockId(x + 1, y, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.Reader.GetBlockId(x, y, z - 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.Reader.GetBlockId(x, y, z + 1) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }

        if (level.Reader.GetBlockId(x, y - 1, z) == Block.Netherrack.id)
        {
            ++netherrackNeighbors;
        }


        int airNeighbors = 0;
        if (level.Reader.IsAir(x - 1, y, z))
        {
            ++airNeighbors;
        }

        if (level.Reader.IsAir(x + 1, y, z))
        {
            ++airNeighbors;
        }

        if (level.Reader.IsAir(x, y, z - 1))
        {
            ++airNeighbors;
        }

        if (level.Reader.IsAir(x, y, z + 1))
        {
            ++airNeighbors;
        }

        if (level.Reader.IsAir(x, y - 1, z))
        {
            ++airNeighbors;
        }

        if (netherrackNeighbors == 4 && airNeighbors == 1)
        {
            level.Writer.SetBlock(x, y, z, _lavaBlockId, 0, false);
            level.TickScheduler.TriggerInstantTick(x, y, z, _lavaBlockId);
        }

        return true;
    }
}
