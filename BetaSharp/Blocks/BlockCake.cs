using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCake : Block
{
    public BlockCake(int id, int textureId) : base(id, textureId, Material.Cake) => setTickRandomly(true);

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        int slicesEaten = blockReader.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        setBoundingBox(minX, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override void setupRenderBoundingBox()
    {
        float edgeInset = 1.0F / 16.0F;
        float height = 0.5F;
        setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, height, 1.0F - edgeInset);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height - edgeInset), (double)((float)(z + 1) - edgeInset));
    }

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        int slicesEaten = world.GetBlockMeta(x, y, z);
        float edgeInset = 1.0F / 16.0F;
        float minX = (float)(1 + slicesEaten * 2) / 16.0F;
        float height = 0.5F;
        return new Box((double)((float)x + minX), (double)y, (double)((float)z + edgeInset), (double)((float)(x + 1) - edgeInset), (double)((float)y + height), (double)((float)(z + 1) - edgeInset));
    }

    public override int getTexture(int side, int meta)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : (meta > 0 && side == 4 ? textureId + 2 : textureId + 1));
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId : (side == 0 ? textureId + 3 : textureId + 1);
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool onUse(OnUseEvent @event)
    {
        if (@event.Player.health < 20)
        {
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

        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        if (@event.Player.health >= 20)
        {
            return;
        }

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

    public override bool canPlaceAt(CanPlaceAtContext evt)
    {
        return !base.canPlaceAt(evt) ? false : canGrow(evt.World.Reader, evt.X, evt.Y, evt.Z);
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (canGrow(@event.World.Reader, @event.X, @event.Y, @event.Z))
        {
            return;
        }

        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override bool canGrow(OnTickEvent @event)
    {
        return canGrow(@event.World.Reader, @event.X, @event.Y, @event.Z);
    }

    private static bool canGrow(IBlockReader world, int x, int y, int z)
    {
        return world.GetMaterial(x, y - 1, z).IsSolid;
    }

    public override int getDroppedItemCount()
    {
        return 0;
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return 0;
    }
}
