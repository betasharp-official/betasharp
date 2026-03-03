using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockIce : BlockBreakable
{

    public BlockIce(int id, int textureId) : base(id, textureId, Material.Ice, false)
    {
        slipperiness = 0.98F;
        setTickRandomly(true);
    }

    public override int getRenderLayer()
    {
        return 1;
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        return base.isSideVisible(iBlockAccess, x, y, z, 1 - side);
    }

    public override void afterBreak(World world, EntityPlayer player, int x, int y, int z, int meta)
    {
        base.afterBreak(world, player, x, y, z, meta);
        Material materialBelow = world.getMaterial(x, y - 1, z);
        if (materialBelow.BlocksMovement || materialBelow.IsFluid)
        {
            world.setBlock(x, y, z, Block.FlowingWater.id);
        }

    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        if (world.getBrightness(LightType.Block, x, y, z) > 11 - Block.BlockLightOpacity[id])
        {
            dropStacks(world, x, y, z, world.getBlockMeta(x, y, z));
            world.setBlock(x, y, z, Block.Water.id);
        }

    }

    public override int getPistonBehavior()
    {
        return 0;
    }
}
