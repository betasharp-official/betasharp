using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockSnow : Block
{
    private const float DropSpread = 0.7F;

    public BlockSnow(int id, int textureId) : base(id, textureId, Material.SnowLayer)
    {
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F / 16.0F, 1.0F);
        SetTickRandomly(true);
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        int meta = world.GetBlockMeta(x, y, z) & 7;
        if (meta >= 3)
        {
            return new Box(x + BoundingBox.MinX, y + BoundingBox.MinY, z + BoundingBox.MinZ, x + BoundingBox.MaxX, y + 0.5F, z + BoundingBox.MaxZ);
        }

        return null;
    }


    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z) & 7;
        float height = 2 * (1 + meta) / 16.0F;
        SetBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, height, 1.0F);
    }

    public override bool CanPlaceAt(CanPlaceAtContext evt)
    {
        int blockBelowId = evt.World.Reader.GetBlockId(evt.X, evt.Y - 1, evt.Z);
        return blockBelowId != 0 && Blocks[blockBelowId].IsOpaque() && evt.World.Reader.GetMaterial(evt.X, evt.Y - 1, evt.Z).BlocksMovement;
    }

    public override void NeighborUpdate(OnTickEvent @event) => BreakIfCannotPlace(@event);

    private bool BreakIfCannotPlace(OnTickEvent @event)
    {
        if (CanPlaceAt(new CanPlaceAtContext(@event.World, 0, @event.X, @event.Y, @event.Z)))
        {
            return true;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        return false;
    }

    public override void OnAfterBreak(OnAfterBreakEvent @event)
    {
        int snowballId = Item.Snowball.id;
        double offsetX = Random.Shared.NextSingle() * DropSpread + (1.0F - DropSpread) * 0.5D;
        double offsetY = Random.Shared.NextSingle() * DropSpread + (1.0F - DropSpread) * 0.5D;
        double offsetZ = Random.Shared.NextSingle() * DropSpread + (1.0F - DropSpread) * 0.5D;
        EntityItem entityItem = new(@event.World, @event.X + offsetX, @event.Y + offsetY, @event.Z + offsetZ, new ItemStack(snowballId, 1, 0))
        {
            delayBeforeCanPickup = 10
        };
        @event.World.Entities.SpawnEntity(entityItem);
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        @event.Player.increaseStat(Stats.Stats.MineBlockStatArray[ID], 1);
    }

    public override int GetDroppedItemId(int blockMeta) => Item.Snowball.id;

    public override int GetDroppedItemCount() => 0;

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.Lighting.GetBrightness(LightType.Block, @event.X, @event.Y, @event.Z) <= 11)
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side) => side == Side.Up ||
                                                                                                     base.IsSideVisible(iBlockReader, x, y, z, side);
}
