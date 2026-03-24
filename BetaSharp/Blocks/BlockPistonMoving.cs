using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonMoving : BlockWithEntity
{
    public BlockPistonMoving(int id) : base(id, Material.Piston) => SetHardness(-1.0F);

    public override BlockEntity? getBlockEntity() => null;

    public override void OnPlaced(OnPlacedEvent @event)
    {
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        BlockEntity? blockEntity = @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z);
        if (blockEntity is BlockEntityPiston piston)
        {
            piston.finish();
        }
        else
        {
            base.OnBreak(@event);
        }
    }

    public override bool CanPlaceAt(CanPlaceAtContext context) => false;

    public override BlockRendererType GetRenderType() => BlockRendererType.Entity;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override bool OnUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote || @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) != null)
        {
            return false;
        }

        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        return true;
    }

    public override int GetDroppedItemId(int blockMeta) => 0;

    public override void DropStacks(OnDropEvent @event)
    {
        (IWorldContext world, int i, int y, int z, _, _) = @event;

        if (world.IsRemote)
        {
            return;
        }

        BlockEntityPiston? piston = world.Entities.GetBlockEntity<BlockEntityPiston>(i, y, z);
        if (piston != null)
        {
            Blocks[piston.getPushedBlockId()]!.DropStacks(new OnDropEvent(world,
                i,
                y,
                z,
                piston.getPushedBlockData()
            ));
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote && @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) == null)
        {
        }
    }

    public static BlockEntity createPistonBlockEntity(int blockId, int blockMeta, int facing, bool extending, bool source) => new BlockEntityPiston(blockId, blockMeta, facing, extending, source);

    public override Box? GetCollisionShape(IBlockReader iBlockReader, EntityManager entities, int x, int y, int z)
    {
        BlockEntityPiston? piston = entities.GetBlockEntity<BlockEntityPiston>(x, y, z);
        if (piston == null)
        {
            return null;
        }

        float var6 = piston.getProgress(0.0F);
        if (piston.isExtending())
        {
            var6 = 1.0F - var6;
        }

        return getPushedBlockCollisionShape(iBlockReader, entities, x, y, z, piston.getPushedBlockId(), var6, piston.getFacing());
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        if (entities is null)
        {
            return;
        }

        BlockEntityPiston? piston = entities.GetBlockEntity<BlockEntityPiston>(x, y, z);
        if (piston == null)
        {
            return;
        }

        Block? block = Blocks[piston.getPushedBlockId()];
        if (block == null || block == this)
        {
            return;
        }

        block.UpdateBoundingBox(blockReader, entities, x, y, z);
        float progress = piston.getProgress(0.0F);
        if (piston.isExtending())
        {
            progress = 1.0F - progress;
        }

        int var8 = piston.getFacing();
        BoundingBox = BoundingBox.Offset(-(double)(PistonConstants.HeadOffsetX(var8) * progress), -(double)(PistonConstants.HeadOffsetY(var8) * progress), -(double)(PistonConstants.HeadOffsetZ(var8) * progress));
    }

    public Box? getPushedBlockCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z, int blockId, float sizeMultiplier, int facing)
    {
        if (blockId == 0 || blockId == Id)
        {
            return null;
        }

        Box? shape = Blocks[blockId]?.GetCollisionShape(world, entities, x, y, z);
        if (shape == null)
        {
            return null;
        }

        Box box = shape.Value;
        box.MinX -= PistonConstants.HeadOffsetX(facing) * sizeMultiplier;
        box.MaxX -= PistonConstants.HeadOffsetX(facing) * sizeMultiplier;
        box.MinY -= PistonConstants.HeadOffsetY(facing) * sizeMultiplier;
        box.MaxY -= PistonConstants.HeadOffsetY(facing) * sizeMultiplier;
        box.MinZ -= PistonConstants.HeadOffsetZ(facing) * sizeMultiplier;
        box.MaxZ -= PistonConstants.HeadOffsetZ(facing) * sizeMultiplier;
        return box;
    }
}
