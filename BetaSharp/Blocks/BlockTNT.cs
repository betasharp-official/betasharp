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

    public override void OnPlaced(OnPlacedEvent @event)
    {
        base.OnPlaced(@event);
        if (!@event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z))
        {
            return;
        }

        OnMetadataChange(new OnMetadataChangeEvent(@event.World, @event.X, @event.Y, @event.Z, 1));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (@event.BlockId <= 0 || !Blocks[@event.BlockId].CanEmitRedstonePower() || !@event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z))
        {
            return;
        }

        OnMetadataChange(new OnMetadataChangeEvent(@event.World, @event.X, @event.Y, @event.Z, 1));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override int GetDroppedItemCount() => 0;

    public override void OnDestroyedByExplosion(OnDestroyedByExplosionEvent @event)
    {
        EntityTNTPrimed entityTntPrimed = new(@event.World, @event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F);
        entityTntPrimed.fuse = @event.World.Random.NextInt(entityTntPrimed.fuse / 4) + entityTntPrimed.fuse / 8;
        @event.World.Entities.SpawnEntity(entityTntPrimed);
    }

    public override void OnMetadataChange(OnMetadataChangeEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        if ((@event.Meta & 1) == 0)
        {
            DropStack(@event.World, @event.X, @event.Y, @event.Z, new ItemStack(TNT.ID, 1, 0));
        }
        else
        {
            EntityTNTPrimed entityTntPrimed = new(@event.World, @event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F);
            @event.World.Entities.SpawnEntity(entityTntPrimed);
            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5F, @event.Y + 0.5F, @event.Z + 0.5F, "random.fuse", 1.0F, 1.0F);
        }
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent ctx)
    {
        if (ctx.Player.getHand() != null && ctx.Player.getHand().ItemId == Item.FlintAndSteel.id)
        {
            ctx.World.Writer.SetBlockMetaWithoutNotifyingNeighbors(ctx.X, ctx.Y, ctx.Z, 1);
        }

        base.OnBlockBreakStart(ctx);
    }

    public override bool OnUse(OnUseEvent ctx) => base.OnUse(ctx);
}
