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

    public override void onTick(WorldBlockView worldView, int x, int y, int z, JavaRandom random, WorldEventBroadcaster broadcaster, bool isRemote)
    {
        if (random.NextInt(100) == 0)
        {
            int tryX = x + random.NextInt(3) - 1;
            int tryY = y + random.NextInt(2) - random.NextInt(2);
            int tryZ = z + random.NextInt(3) - 1;
            if (worldView.isAir(tryX, tryY, tryZ) && canGrow(worldView, tryX, tryY, tryZ))
            {
                if (worldView.isAir(tryX, tryY, tryZ) && canGrow(worldView, tryX, tryY, tryZ))
                {
                    worldView.setBlock(tryX, tryY, tryZ, id);
                }
            }
        }
    }

    protected override bool canPlantOnTop(int id) => BlocksOpaque[id];

    public override bool canGrow(World world, int x, int y, int z) => y >= 0 && y < 128 ? world.getBrightness(x, y, z) < 13 && canPlantOnTop(world.getBlockId(x, y - 1, z)) : false;
}
