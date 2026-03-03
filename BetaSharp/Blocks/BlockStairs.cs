using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

internal class BlockStairs : Block
{

    private Block baseBlock;

    public BlockStairs(int id, Block block) : base(id, block.textureId, block.material)
    {
        baseBlock = block;
        setHardness(block.hardness);
        setResistance(block.resistance / 3.0F);
        setSoundGroup(block.soundGroup);
        setOpacity(255);
    }

    public override void updateBoundingBox(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override Box? getCollisionShape(World world, int x, int y, int z)
    {
        return base.getCollisionShape(world, x, y, z);
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Stairs;
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        return base.isSideVisible(iBlockAccess, x, y, z, side);
    }

    public override void addIntersectingBoundingBox(World world, int x, int y, int z, Box box, List<Box> boxes)
    {
        int meta = world.getBlockMeta(x, y, z);
        if (meta == 0)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 0.5F, 0.5F, 1.0F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
            setBoundingBox(0.5F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
        }
        else if (meta == 1)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 0.5F, 1.0F, 1.0F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
            setBoundingBox(0.5F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
        }
        else if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 0.5F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
            setBoundingBox(0.0F, 0.0F, 0.5F, 1.0F, 1.0F, 1.0F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
        }
        else if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.5F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
            setBoundingBox(0.0F, 0.0F, 0.5F, 1.0F, 0.5F, 1.0F);
            base.addIntersectingBoundingBox(world, x, y, z, box, boxes);
        }

        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override void randomDisplayTick(World world, int x, int y, int z, JavaRandom random)
    {
        baseBlock.randomDisplayTick(world, x, y, z, random);
    }

    public override void onBlockBreakStart(World world, int x, int y, int z, EntityPlayer player)
    {
        baseBlock.onBlockBreakStart(world, x, y, z, player);
    }

    public override void onMetadataChange(World world, int x, int y, int z, int meta)
    {
        baseBlock.onMetadataChange(world, x, y, z, meta);
    }

    public override float getLuminance(IBlockAccess iBlockAccess, int x, int y, int z)
    {
        return baseBlock.getLuminance(iBlockAccess, x, y, z);
    }

    public override float getBlastResistance(Entity entity)
    {
        return baseBlock.getBlastResistance(entity);
    }

    public override int getRenderLayer()
    {
        return baseBlock.getRenderLayer();
    }

    public override int getDroppedItemId(int blockMeta, JavaRandom random)
    {
        return baseBlock.getDroppedItemId(blockMeta, random);
    }

    public override int getDroppedItemCount(JavaRandom random)
    {
        return baseBlock.getDroppedItemCount(random);
    }

    public override int getTexture(int side, int meta)
    {
        return baseBlock.getTexture(side, meta);
    }

    public override int getTexture(int side)
    {
        return baseBlock.getTexture(side);
    }

    public override int getTextureId(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        return baseBlock.getTextureId(iBlockAccess, x, y, z, side);
    }

    public override int getTickRate()
    {
        return baseBlock.getTickRate();
    }

    public override Box getBoundingBox(World world, int x, int y, int z)
    {
        return baseBlock.getBoundingBox(world, x, y, z);
    }

    public override void applyVelocity(World world, int x, int y, int z, Entity entity, Vec3D velocity)
    {
        baseBlock.applyVelocity(world, x, y, z, entity, velocity);
    }

    public override bool hasCollision()
    {
        return baseBlock.hasCollision();
    }

    public override bool hasCollision(int meta, bool allowLiquids)
    {
        return baseBlock.hasCollision(meta, allowLiquids);
    }

    public override bool canPlaceAt(World world, int x, int y, int z)
    {
        return baseBlock.canPlaceAt(world, x, y, z);
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        neighborUpdate(world, x, y, z, 0);
        baseBlock.onPlaced(world, x, y, z);
    }

    public override void onBreak(World world, int x, int y, int z)
    {
        baseBlock.onBreak(world, x, y, z);
    }

    public override void dropStacks(World world, int x, int y, int z, int meta, float luck)
    {
        baseBlock.dropStacks(world, x, y, z, meta, luck);
    }

    public override void onSteppedOn(World world, int x, int y, int z, Entity entity)
    {
        baseBlock.onSteppedOn(world, x, y, z, entity);
    }

    public override void onTick(World world, int x, int y, int z, JavaRandom random)
    {
        baseBlock.onTick(world, x, y, z, random);
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        return baseBlock.onUse(world, x, y, z, player);
    }

    public override void onDestroyedByExplosion(World world, int x, int y, int z)
    {
        baseBlock.onDestroyedByExplosion(world, x, y, z);
    }

    public override void onPlaced(World world, int x, int y, int z, EntityLiving placer)
    {
        int facing = MathHelper.Floor((double)(placer.yaw * 4.0F / 360.0F) + 0.5D) & 3;
        if (facing == 0)
        {
            world.setBlockMeta(x, y, z, 2);
        }

        if (facing == 1)
        {
            world.setBlockMeta(x, y, z, 1);
        }

        if (facing == 2)
        {
            world.setBlockMeta(x, y, z, 3);
        }

        if (facing == 3)
        {
            world.setBlockMeta(x, y, z, 0);
        }

    }
}
