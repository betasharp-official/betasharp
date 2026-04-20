using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class PumpkinPatchFeature : Feature
{
    public override bool Generate(IWorldContext level, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (level.Reader.IsAir(genX, genY, genZ) &&
                level.Reader.GetBlockId(genX, genY - 1, genZ) == Block.GrassBlock.ID &&
                Block.Pumpkin.CanPlaceAt(new CanPlaceAtContext(level, 0, genX, genY, genZ)))
            {
                level.Writer.SetBlockWithoutNotifyingNeighbors(genX, genY, genZ, Block.Pumpkin.ID, rand.NextInt(4), false);
            }
        }

        return true;
    }
}
