using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockCactus : Block
{
    public BlockCactus(int id, int textureId) : base(id, textureId, Material.Cactus) => setTickRandomly(true);

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.Reader.IsAir(@event.X, @event.Y + 1, @event.Z))
        {
            int heightBelow;
            for (heightBelow = 1; @event.World.Reader.GetBlockId(@event.X, @event.Y - heightBelow, @event.Z) == id; ++heightBelow)
            {
            }

            if (heightBelow < 3)
            {
                int growthStage = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
                if (growthStage == 15)
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, id);
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, 0);
                }
                else
                {
                    @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, growthStage + 1);
                }
            }
        }
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        float edgeInset = 1.0F / 16.0F;
        return new Box(x + edgeInset, y, z + edgeInset, x + 1 - edgeInset, y + 1 - edgeInset, z + 1 - edgeInset);
    }

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        float edgeInset = 1.0F / 16.0F;
        return new Box(x + edgeInset, y, z + edgeInset, x + 1 - edgeInset, y + 1, z + 1 - edgeInset);
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId - 1 : side == 0 ? textureId + 1 : textureId;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Cactus;
    }

    public override bool canPlaceAt(CanPlaceAtContext evt)
    {
        return !base.canPlaceAt(evt) ? false : canGrow(evt.World.Reader, evt.X, evt.Y, evt.Z);
    }


    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!canGrow(@event))
        {
            dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
    }

    public override bool canGrow(OnTickEvent @event)
    {
        return canGrow(@event.World.Reader, @event.X, @event.Y, @event.Z);
    }

    private static bool canGrow(WorldReader world, int x, int y, int z)
    {
        if (world.GetMaterial(x - 1, y, z).IsSolid)
        {
            return false;
        }

        if (world.GetMaterial(x + 1, y, z).IsSolid)
        {
            return false;
        }

        if (world.GetMaterial(x, y, z - 1).IsSolid)
        {
            return false;
        }

        if (world.GetMaterial(x, y, z + 1).IsSolid)
        {
            return false;
        }

        int blockBelowId = world.GetBlockId(x, y - 1, z);
        return blockBelowId == Cactus.id || blockBelowId == Sand.id;
    }

    public override void onEntityCollision(OnEntityCollisionEvent ctx)
    {
        ctx.Entity.damage(null, 1);
    }
}
