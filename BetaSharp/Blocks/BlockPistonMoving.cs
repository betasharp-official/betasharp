using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockPistonMoving : BlockWithEntity
{
    public BlockPistonMoving(int id) : base(id, Material.Piston)
    {
        setHardness(-1.0F);
    }

    protected override BlockEntity getBlockEntity()
    {
        return null;
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        BlockEntity var5 = world.getBlockEntity(x, y, z);
        if (var5 != null && var5 is BlockEntityPiston)
        {
            ((BlockEntityPiston)var5).finish();
        }
        else
        {
            base.onBreak(world, x, y, z);
        }

    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return false;
    }

    public override bool canPlaceAt(World world, int x, int y, int z, int side)
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Entity;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (!world.isRemote && world.getBlockEntity(x, y, z) == null)
        {
            world.setBlock(x, y, z, 0);
            return true;
        }
        else
        {
            return false;
        }
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return 0;
    }

    public override void dropStacks(World world, int x, int y, int z, int meta, float luck)
    {
        if (!world.isRemote)
        {
            BlockEntityPiston var7 = getPistonBlockEntity(world, x, y, z);
            if (var7 != null)
            {
                Block.Blocks[var7.getPushedBlockId()].dropStacks(world, x, y, z, var7.getPushedBlockData());
            }
        }
    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        if (!world.isRemote && world.getBlockEntity(x, y, z) == null)
        {
        }

    }

    public static BlockEntity createPistonBlockEntity(int blockId, int blockMeta, int facing, bool extending, bool source)
    {
        return new BlockEntityPiston(blockId, blockMeta, facing, extending, source);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        BlockEntityPiston var5 = getPistonBlockEntity(world, x, y, z);
        if (var5 == null)
        {
            return null;
        }
        else
        {
            float var6 = var5.getProgress(0.0F);
            if (var5.isExtending())
            {
                var6 = 1.0F - var6;
            }

            return getPushedBlockCollisionShape(world, x, y, z, var5.getPushedBlockId(), var6, var5.getFacing());
        }
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        BlockEntityPiston var5 = getPistonBlockEntity(iBlockAccess, x, y, z);
        if (var5 != null)
        {
            Block var6 = Block.Blocks[var5.getPushedBlockId()];
            if (var6 == null || var6 == this)
            {
                return;
            }

            var6.updateBoundingBox(iBlockAccess, x, y, z);
            float var7 = var5.getProgress(0.0F);
            if (var5.isExtending())
            {
                var7 = 1.0F - var7;
            }

            int var8 = var5.getFacing();
            BoundingBox = BoundingBox.Offset(-(double)(PistonConstants.HEAD_OFFSET_X[var8] * var7), -(double)(PistonConstants.HEAD_OFFSET_Y[var8] * var7), -(double)(PistonConstants.HEAD_OFFSET_Z[var8] * var7));
        }

    }

    public Box? getPushedBlockCollisionShape(World world, int x, int y, int z, int blockId, float sizeMultiplier, int facing)
    {
        if (blockId != 0 && blockId != id)
        {
            Box? shape = Block.Blocks[blockId].getCollisionShape(world, x, y, z);
            if (shape == null)
            {
                return null;
            }
            else
            {
                Box res = shape.Value;
                res.MinX -= (double)((float)PistonConstants.HEAD_OFFSET_X[facing] * sizeMultiplier);
                res.MaxX -= (double)((float)PistonConstants.HEAD_OFFSET_X[facing] * sizeMultiplier);
                res.MinY -= (double)((float)PistonConstants.HEAD_OFFSET_Y[facing] * sizeMultiplier);
                res.MaxY -= (double)((float)PistonConstants.HEAD_OFFSET_Y[facing] * sizeMultiplier);
                res.MinZ -= (double)((float)PistonConstants.HEAD_OFFSET_Z[facing] * sizeMultiplier);
                res.MaxZ -= (double)((float)PistonConstants.HEAD_OFFSET_Z[facing] * sizeMultiplier);
                return res;
            }
        }
        else
        {
            return null;
        }
    }

    private BlockEntityPiston getPistonBlockEntity(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        BlockEntity? var5 = iBlockAccess.getBlockEntity(x, y, z);
        return var5 != null && var5 is BlockEntityPiston ? (BlockEntityPiston)var5 : null;
    }
}
