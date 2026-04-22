using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCake : Block
{
    private const float CakeHeight = 0.5F;
    private const float EdgeInset = 1.0F / 16.0F;
    public BlockCake(int id, int textureId) : base(id, textureId, Material.Cake) => setTickRandomly(true);

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int slicesEaten = blockReader.GetBlockMeta(x, y, z);
        float minX = (1 + slicesEaten * 2) / 16.0F;
        setBoundingBox(minX, 0.0F, EdgeInset, 1.0F - EdgeInset, CakeHeight, 1.0F - EdgeInset);
    }

    public override void setupRenderBoundingBox() => setBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, CakeHeight, 1.0F - EdgeInset);

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float minX = (1 + slicesEaten * 2) / 16.0F;
        return new Box(x + minX, y, z + EdgeInset, x + 1 - EdgeInset, y + CakeHeight - EdgeInset, z + 1 - EdgeInset);
    }

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float minX = (1 + slicesEaten * 2) / 16.0F;
        return new Box(x + minX, y, z + EdgeInset, x + 1 - EdgeInset, y + CakeHeight, z + 1 - EdgeInset);
    }

    public override int GetTexture(Side side, int meta)
    {
        if (side == Side.Up)return BlockTextures.Cake;
        if (side == Side.Down)return TextureId + 3;
        if (meta > 0 && side == Side.West)return TextureId + 2;
        return TextureId + 1;
    }

    public override int GetTexture(Side side) => side switch
    {
        Side.Up => TextureId,
        Side.Down => TextureId + 3,
        _ => TextureId + 1
    };

    public override bool isFullCube() => false;

    public override bool isOpaque() => false;

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.Player.Health >= 20) return true;

        @event.Player.heal(3);
        int slicesEaten = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) + 1;
        if (slicesEaten >= 6)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, slicesEaten);
            @event.World.Broadcaster.SetBlocksDirty(@event.X, @event.Y, @event.Z);
        }

        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        if (@event.Player.Health >= 20) return;

        @event.Player.heal(3);
        int slicesEaten = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) + 1;
        if (slicesEaten >= 6)
        {
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
        else
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, slicesEaten);
            @event.World.Broadcaster.SetBlocksDirty(@event.X, @event.Y, @event.Z);
        }
    }

    public override bool canPlaceAt(CanPlaceAtContext evt) => base.canPlaceAt(evt) && canGrow(evt.World.Reader, evt.X, evt.Y, evt.Z);

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (canGrow(@event.World.Reader, @event.X, @event.Y, @event.Z)) return;
        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool canGrow(OnTickEvent @event) => canGrow(@event.World.Reader, @event.X, @event.Y, @event.Z);

    private static bool canGrow(IBlockReader world, int x, int y, int z) => world.GetMaterial(x, y - 1, z).IsSolid;

    public override int getDroppedItemCount() => 0;

    public override int getDroppedItemId(int blockMeta) => 0;
}
