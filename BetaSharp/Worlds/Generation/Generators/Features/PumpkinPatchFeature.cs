using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class PumpkinPatchFeature : Feature
{
    public override bool Generate(IWorldContext level, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + level.random.NextInt(8) - level.random.NextInt(8);
            int genY = y + level.random.NextInt(4) - level.random.NextInt(4);
            int genZ = z + level.random.NextInt(8) - level.random.NextInt(8);
            if (level.BlocksReader.IsAir(genX, genY, genZ) &&
                level.BlocksReader.GetBlockId(genX, genY - 1, genZ) == Block.GrassBlock.id &&
                Block.Pumpkin.canPlaceAt(new CanPlaceAtCtx(level, 0, genX, genY, genZ)))
            {
                level.BlockWriter.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, Block.Pumpkin.id, level.random.NextInt(4), notifyBlockPlaced: false);
            }
        }

        return true;
    }
}
