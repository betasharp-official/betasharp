using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockStairs : Block
{
    private readonly Block _baseBlock;

    public BlockStairs(int id, Block block) : base(id, block.TextureId, block.Material)
    {
        _baseBlock = block;
        SetHardness(block.Hardness);
        SetResistance(block.Resistance / 3.0F);
        SetSoundGroup(block.SoundGroup);
        SetOpacity(255);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Stairs;

    public override void AddIntersectingBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z, Box box, List<Box> boxes)
    {
        int meta = world.GetBlockMeta(x, y, z);
        switch (meta)
        {
            case 0:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 0.5F, 0.5F, 1.0F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                SetBoundingBox(0.5F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                break;
            case 1:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 0.5F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                SetBoundingBox(0.5F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                break;
            case 2:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 0.5F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                SetBoundingBox(0.0F, 0.0F, 0.5F, 1.0F, 1.0F, 1.0F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                break;
            case 3:
                SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.5F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                SetBoundingBox(0.0F, 0.0F, 0.5F, 1.0F, 0.5F, 1.0F);
                base.AddIntersectingBoundingBox(world, entities, x, y, z, box, boxes);
                break;
        }

        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
    }

    public override void RandomDisplayTick(OnTickEvent @event) => _baseBlock.RandomDisplayTick(@event);

    public override void OnBlockBreakStart(OnBlockBreakStartEvent @event) => _baseBlock.OnBlockBreakStart(@event);

    public override void OnMetadataChange(OnMetadataChangeEvent @event) => _baseBlock.OnMetadataChange(@event);

    public override float GetLuminance(ILightProvider lighting, int x, int y, int z) => _baseBlock.GetLuminance(lighting, x, y, z);

    public override float GetBlastResistance(Entity entity) => _baseBlock.GetBlastResistance(entity);

    public override int GetRenderLayer() => _baseBlock.GetRenderLayer();

    public override int GetDroppedItemId(int blockMeta) => _baseBlock.GetDroppedItemId(blockMeta);

    public override int GetDroppedItemCount() => _baseBlock.GetDroppedItemCount();

    public override int GetTexture(Side side, int meta) => _baseBlock.GetTexture(side, meta);

    public override int GetTexture(Side side) => _baseBlock.GetTexture(side);

    public override int GetTextureId(IBlockReader iBlockReader, int x, int y, int z, Side side) => _baseBlock.GetTextureId(iBlockReader, x, y, z, side);

    public override int GetTickRate() => _baseBlock.GetTickRate();

    public override Box GetBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z) => _baseBlock.GetBoundingBox(world, entities, x, y, z);

    public override Vec3D ApplyVelocity(OnApplyVelocityEvent ctx) => _baseBlock.ApplyVelocity(ctx);

    public override bool HasCollision() => _baseBlock.HasCollision();

    public override bool HasCollision(int meta, bool allowLiquids) => _baseBlock.HasCollision(meta, allowLiquids);

    public override bool CanPlaceAt(CanPlaceAtContext evt) => _baseBlock.CanPlaceAt(evt);

    public override void OnPlaced(OnPlacedEvent evt)
    {
        int meta = 0;
        if (evt.Placer != null)
        {
            int facing = MathHelper.Floor(evt.Placer.Yaw * 4.0F / 360.0F + 0.5D) & 3;

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

        evt.World.Writer.SetBlockMeta(evt.X, evt.Y, evt.Z, meta);
        evt.World.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z, ID);
        _baseBlock.OnPlaced(evt);
    }

    public override void OnBreak(OnBreakEvent ctx) => _baseBlock.OnBreak(ctx);

    public override void DropStacks(OnDropEvent ctx) => _baseBlock.DropStacks(ctx);

    public override void OnSteppedOn(OnEntityStepEvent ctx) => _baseBlock.OnSteppedOn(ctx);

    public override void OnTick(OnTickEvent ctx) => _baseBlock.OnTick(ctx);

    public override bool OnUse(OnUseEvent ctx) => _baseBlock.OnUse(ctx);

    public override void OnDestroyedByExplosion(OnDestroyedByExplosionEvent ctx) => _baseBlock.OnDestroyedByExplosion(ctx);
}
