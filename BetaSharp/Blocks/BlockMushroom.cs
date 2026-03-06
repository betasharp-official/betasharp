using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockMushroom : BlockPlant
{
    public BlockMushroom(int i, int j) : base(i, j)
    {
        float halfSize = 0.2F;
        setBoundingBox(0.5F - halfSize, 0.0F, 0.5F - halfSize, 0.5F + halfSize, halfSize * 2.0F, 0.5F + halfSize);
        setTickRandomly(true);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (random.NextInt(100) == 0)
        {
            int tryX = x + random.NextInt(3) - 1;
            int tryY = y + random.NextInt(2) - random.NextInt(2);
            int tryZ = z + random.NextInt(3) - 1;
            if (world.IsAir(tryX, tryY, tryZ) && canGrow(world, tryX, tryY, tryZ))
            {
                if (world.IsAir(tryX, tryY, tryZ) && canGrow(world, tryX, tryY, tryZ))
                {
                    world.SetBlock(tryX, tryY, tryZ, id);
                }
            }
        }

    }

    protected override bool canPlantOnTop(int id)
    {
        return Block.BlocksOpaque[id];
    }

    public override bool canGrow(World world, int x, int y, int z)
    {
        return y >= 0 && y < 128 ? world.GetBrightness(x, y, z) < 13 && canPlantOnTop(world.GetBlockId(x, y - 1, z)) : false;
    }
}
