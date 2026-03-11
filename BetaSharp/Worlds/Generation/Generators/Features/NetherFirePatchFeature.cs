using BetaSharp.Blocks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class NetherFirePatchFeature : Feature
{
    public override bool Generate(IWorldContext level, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + level.random.NextInt(8) - level.random.NextInt(8);
            int genY = y + level.random.NextInt(4) - level.random.NextInt(4);
            int genZ = z + level.random.NextInt(8) - level.random.NextInt(8);
            if (level.BlocksReader.IsAir(genX, genY, genZ) && level.BlocksReader.GetBlockId(genX, genY - 1, genZ) == Block.Netherrack.id)
            {
                level.BlockWriter.SetBlock(genX, genY, genZ, Block.Fire.id, 0, doUpdate: false);
            }
        }

        return true;
    }
}
