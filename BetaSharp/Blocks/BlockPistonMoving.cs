using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockPistonMoving : BlockWithEntity
{
    public BlockPistonMoving(int id) : base(id, Material.Piston) => setHardness(-1.0F);

    public override BlockEntity getBlockEntity() => null;

    public override void onPlaced(OnPlacedEvent @event)
    {
    }

    public override void onBreak(OnBreakEvent @event)
    {
        BlockEntity? entity = @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z);
        if (entity is BlockEntityPiston piston)
        {
            piston.Finish();
        }
        else
        {
            base.onBreak(@event);
        }
    }

    public override bool canPlaceAt(CanPlaceAtContext context) => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Entity;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.World.IsRemote || @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) != null) return false;

        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        return true;
    }

    public override int getDroppedItemId(int blockMeta) => 0;

    public override void dropStacks(OnDropEvent @event)
    {
        if (@event.World.IsRemote) return;

        BlockEntityPiston? piston = @event.World.Entities.GetBlockEntity<BlockEntityPiston>(@event.X, @event.Y, @event.Z);
        if (piston != null)
        {
            Blocks[piston.PushedBlockId].dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, piston.PushedBlockData));
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!@event.World.IsRemote && @event.World.Entities.GetBlockEntity<BlockEntity>(@event.X, @event.Y, @event.Z) == null)
        {
        }
    }

    public static BlockEntity CreatePistonBlockEntity(int blockId, int blockMeta, int facing, bool extending, bool source) => new BlockEntityPiston(blockId, blockMeta, facing, extending, source);

    public override Box? getCollisionShape(IBlockReader iBlockReader, EntityManager entities, int x, int y, int z)
    {
        BlockEntityPiston? piston = entities.GetBlockEntity<BlockEntityPiston>(x, y, z);
        if (piston == null)
        {
            return null;
        }

        float progress = piston.getProgress(0.0F);
        if (piston.IsExtending)
        {
            progress = 1.0F - progress;
        }

        return getPushedBlockCollisionShape(iBlockReader, entities, x, y, z, piston.PushedBlockId, progress, piston.Facing);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        BlockEntityPiston? piston = entities?.GetBlockEntity<BlockEntityPiston>(x, y, z);
        if (piston == null) return;

        Block block = Blocks[piston.PushedBlockId];
        if (block == this) return;

        block.updateBoundingBox(blockReader, entities, x, y, z);
        float progress = piston.getProgress(0.0F);
        if (piston.IsExtending)
        {
            progress = 1.0F - progress;
        }

        int facing = piston.Facing;
        BoundingBox = BoundingBox.Offset(-(double)(PistonConstants.HeadOffsetX[facing] * progress), -(double)(PistonConstants.HeadOffsetY[facing] * progress), -(double)(PistonConstants.HeadOffsetZ[facing] * progress));
    }

    public Box? getPushedBlockCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z, int blockId, float sizeMultiplier, int facing)
    {
        if (blockId == 0 || blockId == id) return null;

        Box? shape = Blocks[blockId].getCollisionShape(world, entities, x, y, z);
        if (shape == null) return null;

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
