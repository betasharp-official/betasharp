using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class SpringFeature : Feature
{
    private readonly int _liquidBlockId;

    public SpringFeature(int liquidBlockId) => _liquidBlockId = liquidBlockId;

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        if (level.Reader.GetBlockId(x, y + 1, z) != Block.Stone.id)
        {
            return false;
        }

        if (level.Reader.GetBlockId(x, y - 1, z) != Block.Stone.id)
        {
            return false;
        }

        int targetId = level.Reader.GetBlockId(x, y, z);
        if (targetId != 0 && targetId != Block.Stone.id)
        {
            return false;
        }

        int stoneNeighbors = 0;
        if (level.Reader.GetBlockId(x - 1, y, z) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (level.Reader.GetBlockId(x + 1, y, z) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (level.Reader.GetBlockId(x, y, z - 1) == Block.Stone.id)
        {
            ++stoneNeighbors;
        }

        if (level.Reader.GetBlockId(x, y, z + 1) == Block.Stone.id)
        {
            ++stoneNeighbors;
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


        if (stoneNeighbors == 3 && airNeighbors == 1)
        {
            level.Writer.SetBlock(x, y, z, _liquidBlockId, 0, false);
            level.TickScheduler.TriggerInstantTick(x, y, z, _liquidBlockId);
        }

        return true;
    }
}
