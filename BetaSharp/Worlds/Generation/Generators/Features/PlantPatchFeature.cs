using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class PlantPatchFeature : Feature
{
    private readonly int plantBlockId;

    public PlantPatchFeature(int plantBlockId) => this.plantBlockId = plantBlockId;

    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (level.Reader.IsAir(genX, genY, genZ) &&
                ((BlockPlant)Block.Blocks[plantBlockId]).CanGrow(new OnTickEvent(level, genX, genY, genZ, level.Reader.GetBlockMeta(genX, genY, genZ), level.Reader.GetBlockId(genX, genY, genZ))))
            {
                level.Writer.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, plantBlockId, 0, false);
            }
        }

        return true;
    }
}
