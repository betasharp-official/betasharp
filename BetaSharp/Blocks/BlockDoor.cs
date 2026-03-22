using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDoor : Block
{
    public BlockDoor(int id, Material material) : base(id, material)
    {
        textureId = 97;
        if (material == Material.Metal)
        {
            ++textureId;
        }

        float halfWidth = 0.5F;
        float height = 1.0F;
        setBoundingBox(0.5F - halfWidth, 0.0F, 0.5F - halfWidth, 0.5F + halfWidth, height, 0.5F + halfWidth);
    }

    public override int getTexture(int side, int meta)
    {
        if (side != 0 && side != 1)
        {
            int facing = setOpen(meta);
            if ((facing == 0 || facing == 2) ^ (side <= 3))
            {
                return this.textureId;
            }

            int textureIndex = facing / 2 + ((side & 1) ^ facing);
            textureIndex += (meta & 4) / 4;
            int textureId = this.textureId - (meta & 8) * 2;
            if ((textureIndex & 1) != 0)
            {
                textureId = -textureId;
            }

            return textureId;
        }

        return textureId;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    public override BlockRendererType getRenderType()
    {
        return BlockRendererType.Door;
    }

    public override Box getBoundingBox(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getBoundingBox(world, entities, x, y, z);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        updateBoundingBox(world, x, y, z);
        return base.getCollisionShape(world, entities, x, y, z);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z) => rotate(setOpen(blockReader.GetBlockMeta(x, y, z)));

    public void rotate(int meta)
    {
        float thickness = 3.0F / 16.0F;
        setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 2.0F, 1.0F);
        if (meta == 0)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, thickness);
        }

        if (meta == 1)
        {
            setBoundingBox(1.0F - thickness, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 2)
        {
            setBoundingBox(0.0F, 0.0F, 1.0F - thickness, 1.0F, 1.0F, 1.0F);
        }

        if (meta == 3)
        {
            setBoundingBox(0.0F, 0.0F, 0.0F, thickness, 1.0F, 1.0F);
        }
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event) => updateDorState(@event.World, @event.X, @event.Y, @event.Z);

    private bool updateDorState(IWorldContext world, int x, int y, int z)
    {
        if (material == Material.Metal)
        {
            return true;
        }

        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == id)
            {
                updateDorState(world, x, y - 1, z);
            }

            return true;
        }

        if (world.Reader.GetBlockId(x, y + 1, z) == id)
        {
            world.Writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
        }

        world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
        world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
        world.Broadcaster.WorldEvent(1003, x, y, z, 0);
        return true;
    }


    public override bool onUse(OnUseEvent @event) => updateDorState(@event.World, @event.X, @event.Y, @event.Z);

    public void setOpen(IWorldContext world, int x, int y, int z, bool open)
    {
        int meta = world.Reader.GetBlockMeta(x, y, z);
        if ((meta & 8) != 0)
        {
            if (world.Reader.GetBlockId(x, y - 1, z) == id)
            {
                setOpen(world, x, y - 1, z, open);
            }
        }
        else
        {
            bool isOpen = (world.Reader.GetBlockMeta(x, y, z) & 4) > 0;
            if (isOpen != open)
            {
                if (world.Reader.GetBlockId(x, y + 1, z) == id)
                {
                    world.Writer.SetBlockMeta(x, y + 1, z, (meta ^ 4) + 8);
                }

                world.Writer.SetBlockMeta(x, y, z, meta ^ 4);
                world.Broadcaster.SetBlocksDirty(x, y - 1, z, x, y, z);
                world.Broadcaster.WorldEvent(1003, x, y, z, 0);
            }
        }
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) != 0)
        {
            if (@event.World.Reader.GetBlockId(@event.X, @event.Y - 1, @event.Z) != id)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            }

            if (@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower())
            {
                neighborUpdate(@event);
            }
        }
        else
        {
            bool wasBroken = false;
            if (@event.World.Reader.GetBlockId(@event.X, @event.Y + 1, @event.Z) != id)
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                wasBroken = true;
            }

            if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z))
            {
                @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
                wasBroken = true;
                if (@event.World.Reader.GetBlockId(@event.X, @event.Y + 1, @event.Z) == id)
                {
                    @event.World.Writer.SetBlock(@event.X, @event.Y + 1, @event.Z, 0);
                }
            }

            if (wasBroken)
            {
                if (!@event.World.IsRemote)
                {
                    dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, meta));
                }
            }
            else if (@event.BlockId > 0 && Blocks[@event.BlockId].canEmitRedstonePower())
            {
                bool isPowered = @event.World.Redstone.IsPowered(@event.X, @event.Y, @event.Z) || @event.World.Redstone.IsPowered(@event.X, @event.Y + 1, @event.Z);
                setOpen(@event.World, @event.X, @event.Y, @event.Z, isPowered);
            }
        }
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return (blockMeta & 8) != 0 ? 0 : material == Material.Metal ? Item.IronDoor.id : Item.WoodenDoor.id;
    }

    public override HitResult raycast(IBlockReader world, EntityManager entities, int x, int y, int z, Vec3D startPos, Vec3D endPos)
    {
        updateBoundingBox(world, entities, x, y, z);
        return base.raycast(world, entities, x, y, z, startPos, endPos);
    }

    public int setOpen(int meta)
    {
        return (meta & 4) == 0 ? (meta - 1) & 3 : meta & 3;
    }

    public override bool canPlaceAt(CanPlaceAtContext evt)
    {
        return evt.Y >= 127 ? false : evt.World.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z) && base.canPlaceAt(evt) && base.canPlaceAt(evt);
    }

    public static bool isOpen(int meta)
    {
        return (meta & 4) != 0;
    }

    public override int getPistonBehavior()
    {
        return 1;
    }
}
