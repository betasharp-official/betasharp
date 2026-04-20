using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonMoving : BlockWithEntity
{
    public BlockPistonMoving(int id) : base(id, Material.Piston) => SetHardness(-1.0F);

    public override BlockEntity GetBlockEntity() => null;

    public override void OnPlaced(OnPlacedEvent @event)
    {
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        BlockEntity? entity = @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z);
        if (entity is BlockEntityPiston piston)
        {
            piston.Finish();
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
        if (@event.World.IsRemote)
        {
            return;
        }

        BlockEntityPiston? piston = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(@event.X, @event.Y, @event.Z);
        if (piston != null)
        {
            Blocks[piston.PushedBlockId].DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, piston.PushedBlockData));
        }
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote && @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) == null)
        {
        }
    }

    public static BlockEntity CreatePistonBlockEntity(int blockId, int blockMeta, int facing, bool extending, bool source) => new BlockEntityPiston(blockId, blockMeta, facing, extending, source);

    public override Box? GetCollisionShape(IBlockReader iBlockReader, EntityManager entities, int x, int y, int z)
    {
        BlockEntityPiston? piston = entities.GetBlockEntity<BlockEntityPiston>(x, y, z);
        if (piston == null)
        {
            return null;
        }

        float progress = piston.GetProgress(0.0F);
        if (piston.IsExtending)
        {
            progress = 1.0F - progress;
        }

        return GetPushedBlockCollisionShape(iBlockReader, entities, x, y, z, piston.PushedBlockId, progress, piston.Facing);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        BlockEntityPiston? piston = entities?.GetBlockEntity<BlockEntityPiston>(x, y, z);
        if (piston == null)
        {
            return;
        }

        Block block = Blocks[piston.PushedBlockId];
        if (block == this)
        {
            return;
        }

        block.UpdateBoundingBox(blockReader, entities, x, y, z);
        float progress = piston.GetProgress(0.0F);
        if (piston.IsExtending)
        {
            progress = 1.0F - progress;
        }

        int facing = piston.Facing;
        BoundingBox = BoundingBox.Offset(-(double)(PistonConstants.HeadOffsetX[facing] * progress), -(double)(PistonConstants.HeadOffsetY[facing] * progress), -(double)(PistonConstants.HeadOffsetZ[facing] * progress));
    }

    public Box? GetPushedBlockCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z, int blockId, float sizeMultiplier, int facing)
    {
        if (blockId == 0 || blockId == ID)
        {
            return null;
        }

        Box? shape = Blocks[blockId].GetCollisionShape(world, entities, x, y, z);
        if (shape == null)
        {
            return null;
        }

        Box res = shape.Value;
        res.MinX -= PistonConstants.HeadOffsetX[facing] * sizeMultiplier;
        res.MaxX -= PistonConstants.HeadOffsetX[facing] * sizeMultiplier;
        res.MinY -= PistonConstants.HeadOffsetY[facing] * sizeMultiplier;
        res.MaxY -= PistonConstants.HeadOffsetY[facing] * sizeMultiplier;
        res.MinZ -= PistonConstants.HeadOffsetZ[facing] * sizeMultiplier;
        res.MaxZ -= PistonConstants.HeadOffsetZ[facing] * sizeMultiplier;
        return res;
    }
}
