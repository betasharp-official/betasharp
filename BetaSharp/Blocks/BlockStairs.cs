using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockStairs : Block
{
    private readonly Block baseBlock;

    public BlockStairs(int id, Block block) : base(id, block.textureId, block.material)
    {
        baseBlock = block;
        setHardness(block.hardness);
        setResistance(block.resistance / 3.0F);
        setSoundGroup(block.soundGroup);
        setOpacity(255);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z) => setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => base.getCollisionShape(world, x, y, z);

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Stairs;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side) => base.isSideVisible(iBlockReader, x, y, z, side);

    public override void addIntersectingBoundingBox(IBlockReader world, int x, int y, int z, Box box, List<Box> boxes)
    {
        // Fixed capitalization on GetBlockMeta
        int meta = world.GetBlockMeta(x, y, z);
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

    // Migrated to OnTickEvt
    public override void randomDisplayTick(OnTickEvt evt) => baseBlock.randomDisplayTick(evt);

    // Migrated to OnBlockBreakStartEvt
    public override void onBlockBreakStart(OnBlockBreakStartEvt evt) => baseBlock.onBlockBreakStart(evt);

    public override void onMetadataChange(OnMetadataChangeEvt evt) => baseBlock.onMetadataChange(evt);

    public override float getLuminance(ILightProvider lighting, int x, int y, int z) => baseBlock.getLuminance(lighting, x, y, z);

    public override float getBlastResistance(Entity entity) => baseBlock.getBlastResistance(entity);

    public override int getRenderLayer() => baseBlock.getRenderLayer();

    public override int getDroppedItemId(int blockMeta) => baseBlock.getDroppedItemId(blockMeta);

    public override int getDroppedItemCount() => baseBlock.getDroppedItemCount();

    public override int getTexture(int side, int meta) => baseBlock.getTexture(side, meta);

    public override int getTexture(int side) => baseBlock.getTexture(side);

    public override int getTextureId(IBlockReader iBlockReader, int x, int y, int z, int side) => baseBlock.getTextureId(iBlockReader, x, y, z, side);

    public override int getTickRate() => baseBlock.getTickRate();

    public override Box getBoundingBox(IBlockReader world, int x, int y, int z) => baseBlock.getBoundingBox(world, x, y, z);

    public override Vec3D applyVelocity(OnApplyVelocityEvt ctx) => baseBlock.applyVelocity(ctx);

    public override bool hasCollision() => baseBlock.hasCollision();

    public override bool hasCollision(int meta, bool allowLiquids) => baseBlock.hasCollision(meta, allowLiquids);

    public override bool canPlaceAt(CanPlaceAtCtx evt) => baseBlock.canPlaceAt(evt);

    public override void onPlaced(OnPlacedEvt evt)
    {
        int meta = 0;
        if (evt.Placer != null)
        {
            // 1. Calculate facing based on placer entity yaw
            int facing = MathHelper.Floor(evt.Placer.yaw * 4.0F / 360.0F + 0.5D) & 3;

            if (facing == 0)
            {
                meta = 2;
            }

            if (facing == 1)
            {
                meta = 1;
            }

            if (facing == 2)
            {
                meta = 3;
            }

            if (facing == 3)
            {
                meta = 0;
            }
        }

        evt.Level.BlockWriter.SetBlockMeta(evt.X, evt.Y, evt.Z, meta);
        evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z, id);
        baseBlock.onPlaced(evt);
    }

    // Migrated to OnBreakEvt
    public override void onBreak(OnBreakEvt ctx) => baseBlock.onBreak(ctx);

    public override void dropStacks(OnDropEvt ctx) => baseBlock.dropStacks(ctx);

    public override void onSteppedOn(OnEntityStepEvt ctx) => baseBlock.onSteppedOn(ctx);

    public override void onTick(OnTickEvt ctx) => baseBlock.onTick(ctx);

    // Migrated to OnUseEvt
    public override bool onUse(OnUseEvt ctx) => baseBlock.onUse(ctx);

    public override void onDestroyedByExplosion(OnDestroyedByExplosionEvt ctx) => baseBlock.onDestroyedByExplosion(ctx);
}
