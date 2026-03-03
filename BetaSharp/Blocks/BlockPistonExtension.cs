using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockPistonExtension : Block
{
    private int pistonHeadSprite = -1;

    public BlockPistonExtension(int id, int textureId) : base(id, textureId, Material.Piston)
    {
        setSoundGroup(SoundStoneFootstep);
        setHardness(0.5F);
    }

    public void setSprite(int sprite)
    {
        pistonHeadSprite = sprite;
    }

    public void clearSprite()
    {
        pistonHeadSprite = -1;
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        base.onBreak(world, x, y, z);
        int var5 = world.getBlockMeta(x, y, z);
        int var6 = PistonConstants.field_31057_a[getFacing(var5)];
        x += PistonConstants.HEAD_OFFSET_X[var6];
        y += PistonConstants.HEAD_OFFSET_Y[var6];
        z += PistonConstants.HEAD_OFFSET_Z[var6];
        int var7 = world.getBlockId(x, y, z);
        if (var7 == Block.Piston.id || var7 == Block.StickyPiston.id)
        {
            var5 = world.getBlockMeta(x, y, z);
            if (BlockPistonBase.isExtended(var5))
            {
                Block.Blocks[var7].dropStacks(world, x, y, z, var5);
                world.setBlock(x, y, z, 0);
            }
        }

    }

    public override int getTexture(int side, int meta)
    {
        int var3 = getFacing(meta);
        return side == var3 ? (pistonHeadSprite >= 0 ? pistonHeadSprite : ((meta & 8) != 0 ? textureId - 1 : textureId)) : (side == PistonConstants.field_31057_a[var3] ? 107 : 108);
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.PistonExtension;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return false;
    }

    public override bool canPlaceAt(World world, int x, int y, int z, int side)
    {
        return false;
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return 0;
    }

    public override void addIntersectingBoundingBox(World world, int x, int y, int z, Box box, List<Box> boxes)
    {
        int var7 = world.getBlockMeta(x, y, z);
        switch (getFacing(var7))
        {
            case 0:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                setBoundingBox(6.0F / 16.0F, 0.25F, 6.0F / 16.0F, 10.0F / 16.0F, 1.0F, 10.0F / 16.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                break;
            case 1:
                setBoundingBox(0.0F, 12.0F / 16.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                setBoundingBox(6.0F / 16.0F, 0.0F, 6.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F, 10.0F / 16.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                break;
            case 2:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                setBoundingBox(0.25F, 6.0F / 16.0F, 0.25F, 12.0F / 16.0F, 10.0F / 16.0F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                break;
            case 3:
                setBoundingBox(0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                setBoundingBox(0.25F, 6.0F / 16.0F, 0.0F, 12.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                break;
            case 4:
                setBoundingBox(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                setBoundingBox(6.0F / 16.0F, 0.25F, 0.25F, 10.0F / 16.0F, 12.0F / 16.0F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                break;
            case 5:
                setBoundingBox(12.0F / 16.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                setBoundingBox(0.0F, 6.0F / 16.0F, 0.25F, 12.0F / 16.0F, 10.0F / 16.0F, 12.0F / 16.0F);
                base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
                break;
        }

        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        int var5 = iBlockAccess.getBlockMeta(x, y, z);
        switch (getFacing(var5))
        {
            case 0:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                break;
            case 1:
                setBoundingBox(0.0F, 12.0F / 16.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 2:
                setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                break;
            case 3:
                setBoundingBox(0.0F, 0.0F, 12.0F / 16.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 4:
                setBoundingBox(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                break;
            case 5:
                setBoundingBox(12.0F / 16.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
        }

    }

    public override void neighborUpdate(World world, int x, int y, int z, int id)
    {
        int var6 = getFacing(world.getBlockMeta(x, y, z));
        int var7 = world.getBlockId(x - PistonConstants.HEAD_OFFSET_X[var6], y - PistonConstants.HEAD_OFFSET_Y[var6], z - PistonConstants.HEAD_OFFSET_Z[var6]);
        if (var7 != Block.Piston.id && var7 != Block.StickyPiston.id)
        {
            world.setBlock(x, y, z, 0);
        }
        else
        {
            Block.Blocks[var7].neighborUpdate(world, x - PistonConstants.HEAD_OFFSET_X[var6], y - PistonConstants.HEAD_OFFSET_Y[var6], z - PistonConstants.HEAD_OFFSET_Z[var6], id);
        }

    }

    public static int getFacing(int meta)
    {
        return meta & 7;
    }
}
