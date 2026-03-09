using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class PlantPatchFeature : Feature
{
    private readonly int plantBlockId;

    public PlantPatchFeature(int plantBlockId) => this.plantBlockId = plantBlockId;

    public override bool Generate(IBlockWorldContext level, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + level.random.NextInt(8) - level.random.NextInt(8);
            int genY = y + level.random.NextInt(4) - level.random.NextInt(4);
            int genZ = z + level.random.NextInt(8) - level.random.NextInt(8);
            if (level.BlocksReader.IsAir(genX, genY, genZ) && ((BlockPlant)Block.Blocks[plantBlockId]).canGrow(new OnTickEvt(level, genX, genY, genZ, level.BlocksReader.GetBlockMeta(genX, genY, genZ), level.BlocksReader.GetBlockId(genX, genY, genZ))))
            {
                level.BlockWriter.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, plantBlockId);
            }
        }

        return true;
    }
}
