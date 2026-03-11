using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SpringFeature : Feature
{
    private readonly int _liquidBlockId;

    public SpringFeature(int liquidBlockId) => _liquidBlockId = liquidBlockId;

    public override bool Generate(IWorldContext level, int x, int y, int z)
    {
        if (level.BlocksReader.GetBlockId(x, y + 1, z) != Block.Stone.id)
        {
            return false;
        }

        if (level.BlocksReader.GetBlockId(x, y - 1, z) != Block.Stone.id)
        {
            return false;
        }

        int targetId = level.BlocksReader.GetBlockId(x, y, z);
        if (targetId != 0 && targetId != Block.Stone.id)
        {
            return false;
        }

        int stoneNeighbors = 0;
        if (level.BlocksReader.GetBlockId(x - 1, y, z) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x + 1, y, z) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x, y, z - 1) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (level.BlocksReader.GetBlockId(x, y, z + 1) == Block.Stone.id)
        {
            ++stoneNeighbors;
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


        if (stoneNeighbors == 3 && airNeighbors == 1)
        {
            level.BlockWriter.SetBlock(x, y, z, _liquidBlockId, 0, doUpdate: false);
            level.TickScheduler.TriggerInstantTick(x, y, z, _liquidBlockId);
        }

        return true;
    }
}
