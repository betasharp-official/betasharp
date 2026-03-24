using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockTNT : Block
{
    public BlockTNT(int id, int textureId) : base(id, textureId, Material.Tnt)
    {
    }

    public override int getTexture(int side) => side == 0 ? textureId + 2 : side == 1 ? textureId + 1 : textureId;

    public override void onPlaced(OnPlacedEvent @event)
    {
        base.onPlaced(@event);
        if (@event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z))
        {
            onMetadataChange(new OnMetadataChangeEvent(@event.World, @event.X, @event.Y, @event.Z, 1));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower() && @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z))
        {
            onMetadataChange(new OnMetadataChangeEvent(@event.World, @event.X, @event.Y, @event.Z, 1));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
    }

    public override int getDroppedItemCount() => 0;

    public override void onDestroyedByExplosion(OnDestroyedByExplosionEvent @event)
    {
        EntityTNTPrimed entityTNTPrimed = new(@event.World, @event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F);
        entityTNTPrimed.fuse = @event.World.Random.NextInt(entityTNTPrimed.fuse / 4) + entityTNTPrimed.fuse / 8;
        @event.World.Entities.SpawnEntity(entityTNTPrimed);
    }

    public override void onMetadataChange(OnMetadataChangeEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            if ((@event.Meta & 1) == 0)
            {
                dropStack(@event.World, @event.X, @event.Y, @event.Z, new ItemStack(TNT.id, 1, 0));
            }
            else
            {
                EntityTNTPrimed entityTNTPrimed = new EntityTNTPrimed(@event.World, @event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F);
                @event.World.Entities.SpawnEntity(entityTNTPrimed);
                @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F, "random.fuse", 1.0F, 1.0F);
            }
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent ctx)
    {
        if (ctx.Player.getHand() != null && ctx.Player.getHand().itemId == Item.FlintAndSteel.id)
        {
            ctx.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, 1);
        }

        base.onBlockBreakStart(ctx);
    }

    public override bool onUse(OnUseEvent ctx) => base.onUse(ctx);
}
