using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Worlds.Generation.Generators.Features;

internal class NetherFirePatchFeature : Feature
{
    public override bool Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        for (int i = 0; i < 64; ++i)
        {
            int genX = x + rand.NextInt(8) - rand.NextInt(8);
            int genY = y + rand.NextInt(4) - rand.NextInt(4);
            int genZ = z + rand.NextInt(8) - rand.NextInt(8);
            if (world.IsAir(genX, genY, genZ) && world.GetBlockId(genX, genY - 1, genZ) == Block.Netherrack.id)
            {
                world.SetBlock(genX, genY, genZ, Block.Fire.id);
            }
        }

        return true;
    }
}
