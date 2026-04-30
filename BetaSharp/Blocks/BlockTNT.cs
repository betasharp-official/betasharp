using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockTNT(int id, int textureId) : Block(id, textureId, Material.Tnt)
{
    public override int GetTexture(Side side) => side switch
    {
        Side.Down => BlockTextures.TntBottom,
        Side.Up => BlockTextures.TntTop,
        _ => BlockTextures.TntSide
    };

    public override void onPlaced(OnPlacedEvent @event)
    {
        base.onPlaced(@event);
        if (!@event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z)) return;

        onMetadataChange(new OnMetadataChangeEvent(@event.World, @event.X, @event.Y, @event.Z, 1));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (@event.BlockId <= 0 || !Blocks[@event.BlockId].canEmitRedstonePower() || !@event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z))
            return;

        onMetadataChange(new OnMetadataChangeEvent(@event.World, @event.X, @event.Y, @event.Z, 1));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override int getDroppedItemCount() => 0;

    public override void onDestroyedByExplosion(OnDestroyedByExplosionEvent @event)
    {
        EntityTntPrimed entityTntPrimed = new(@event.World, @event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F);
        entityTntPrimed.Fuse = @event.World.Random.NextInt(entityTntPrimed.Fuse / 4) + entityTntPrimed.Fuse / 8;
        @event.World.Entities.SpawnEntity(entityTntPrimed);
    }

    public override void onMetadataChange(OnMetadataChangeEvent @event)
    {
        if (@event.World.IsRemote) return;

        if ((@event.Meta & 1) == 0)
        {
            dropStack(@event.World, @event.X, @event.Y, @event.Z, new ItemStack(TNT.id, 1, 0));
        }
        else
        {
            EntityTntPrimed entityTntPrimed = new(@event.World, @event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F);
            @event.World.Entities.SpawnEntity(entityTntPrimed);
            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F, "random.fuse", 1.0F, 1.0F);
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent ctx)
    {
        if (ctx.Player.GetHand() != null && ctx.Player.GetHand().ItemId == Item.FlintAndSteel.id)
        {
            ctx.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, 1);
        }

        base.onBlockBreakStart(ctx);
    }

    public override bool onUse(OnUseEvent ctx) => base.onUse(ctx);
}
