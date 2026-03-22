using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockButton : Block
{
    public BlockButton(int id, int textureId) : base(id, textureId, Material.PistonBreakable)
    {
        setTickRandomly(true);
    }

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z)
    {
        return null;
    }

    public override int getTickRate()
    {
        return 20;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isFullCube()
    {
        return false;
    }

    private bool IsValidPlacementSide(IBlockReader read, int x, int y, int z, int side = 0)
    {
        if (side == 2)
        {
            return side == 2 && read.ShouldSuffocate(x, y, z + 1) ? true : side == 3 && read.ShouldSuffocate(x, y, z - 1) ? true : side == 4 && read.ShouldSuffocate(x + 1, y, z) ? true : side == 5 && read.ShouldSuffocate(x - 1, y, z);
        }

        return read.ShouldSuffocate(x - 1, y, z) ? true : read.ShouldSuffocate(x + 1, y, z) ? true : read.ShouldSuffocate(x, y, z - 1) ? true : read.ShouldSuffocate(x, y, z + 1);
    }

    public override bool canPlaceAt(CanPlaceAtContext context)
    {
        return IsValidPlacementSide(context.World.Reader, context.X, context.Y, context.Z, context.Direction);
    }

    public override void onPlaced(OnPlacedEvent evt)
    {
        int facing = evt.World.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        int pressedBit = facing & 8;
        facing &= 7;
        if (evt.Direction == 2 && evt.World.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1))
        {
            facing = 4;
        }
        else if (evt.Direction == 3 && evt.World.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1))
        {
            facing = 3;
        }
        else if (evt.Direction == 4 && evt.World.Reader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z))
        {
            facing = 2;
        }
        else if (evt.Direction == 5 && evt.World.Reader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z))
        {
            facing = 1;
        }
        else
        {
            facing = getPlacementSide(evt.World.Reader, evt.X, evt.Y, evt.Z);
        }

        evt.World.Writer.SetBlockMeta(evt.X, evt.Y, evt.Z, facing + pressedBit);
    }

    private int getPlacementSide(IBlockReader world, int x, int y, int z)
    {
        return world.ShouldSuffocate(x - 1, y, z) ? 1 : world.ShouldSuffocate(x + 1, y, z) ? 2 : world.ShouldSuffocate(x, y, z - 1) ? 3 : world.ShouldSuffocate(x, y, z + 1) ? 4 : 1;
    }

    public override void neighborUpdate(OnTickEvent @event)
    {
        if (!breakIfCannotPlaceAt(@event))
        {
            return;
        }

        int facing = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) & 7;
        bool shouldBreak = false;
        if (!@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z) && facing == 1)
        {
            shouldBreak = true;
        }

        if (!@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z) && facing == 2)
        {
            shouldBreak = true;
        }

        if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1) && facing == 3)
        {
            shouldBreak = true;
        }

        if (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1) && facing == 4)
        {
            shouldBreak = true;
        }

        if (shouldBreak)
        {
            dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        }
    }

    private bool breakIfCannotPlaceAt(OnTickEvent @event)
    {
        if (!IsValidPlacementSide(@event.World.Reader, @event.X, @event.Y, @event.Z))
        {
            dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            return false;
        }

        return true;
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        int facing = meta & 7;
        bool isPressed = (meta & 8) > 0;
        float minY = 6.0F / 16.0F;
        float maxY = 10.0F / 16.0F;
        float halfWidth = 3.0F / 16.0F;
        float thickness = 2.0F / 16.0F;
        if (isPressed)
        {
            thickness = 1.0F / 16.0F;
        }

        if (facing == 1)
        {
            setBoundingBox(0.0F, minY, 0.5F - halfWidth, thickness, maxY, 0.5F + halfWidth);
        }
        else if (facing == 2)
        {
            setBoundingBox(1.0F - thickness, minY, 0.5F - halfWidth, 1.0F, maxY, 0.5F + halfWidth);
        }
        else if (facing == 3)
        {
            setBoundingBox(0.5F - halfWidth, minY, 0.0F, 0.5F + halfWidth, maxY, thickness);
        }
        else if (facing == 4)
        {
            setBoundingBox(0.5F - halfWidth, minY, 1.0F - thickness, 0.5F + halfWidth, maxY, 1.0F);
        }
    }


    private bool updateState(IWorldContext level, int x, int y, int z)
    {
        int meta = level.Reader.GetBlockMeta(x, y, z);
        int facing = meta & 7;
        int pressToggle = 8 - (meta & 8);
        if (pressToggle == 0)
        {
            return true;
        }

        level.Writer.SetBlockMeta(x, y, z, facing + pressToggle);
        level.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
        level.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "random.click", 0.3F, 0.6F);
        level.Broadcaster.NotifyNeighbors(x, y, z, id);
        if (facing == 1)
        {
            level.Broadcaster.NotifyNeighbors(x - 1, y, z, id);
        }
        else if (facing == 2)
        {
            level.Broadcaster.NotifyNeighbors(x + 1, y, z, id);
        }
        else if (facing == 3)
        {
            level.Broadcaster.NotifyNeighbors(x, y, z - 1, id);
        }
        else if (facing == 4)
        {
            level.Broadcaster.NotifyNeighbors(x, y, z + 1, id);
        }
        else
        {
            level.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
        }

        level.TickScheduler.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        return true;
    }

    public override void onBlockBreakStart(OnBlockBreakStartEvent @event)
    {
        updateState(@event.World, @event.X, @event.Y, @event.Z);
    }

    public override bool onUse(OnUseEvent @event)
    {
        return updateState(@event.World, @event.X, @event.Y, @event.Z);
    }

    public override void onBreak(OnBreakEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) > 0)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, id);
            int facing = meta & 7;
            if (facing == 1)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, id);
            }
            else if (facing == 2)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, id);
            }
            else if (facing == 3)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, id);
            }
            else if (facing == 4)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, id);
            }
            else
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
            }
        }

        base.onBreak(@event);
    }

    public override bool isPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        return (reader.GetBlockMeta(x, y, z) & 8) > 0;
    }

    public override bool isStrongPoweringSide(IBlockReader read, int x, int y, int z, int side)
    {
        int meta = read.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int facing = meta & 7;
        return facing == 5 && side == 1 ? true : facing == 4 && side == 2 ? true : facing == 3 && side == 3 ? true : facing == 2 && side == 4 ? true : facing == 1 && side == 5;
    }

    public override bool canEmitRedstonePower()
    {
        return true;
    }

    public override void onTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) != 0)
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta & 7);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, id);
            int facing = meta & 7;
            if (facing == 1)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, id);
            }
            else if (facing == 2)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, id);
            }
            else if (facing == 3)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, id);
            }
            else if (facing == 4)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, id);
            }
            else
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
            }

            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "random.click", 0.3F, 0.5F);
            @event.World.Broadcaster.SetBlocksDirty(@event.X, @event.Y, @event.Z);
        }
    }

    public override void setupRenderBoundingBox()
    {
        float halfWidth = 3.0F / 16.0F;
        float halfHeight = 2.0F / 16.0F;
        float halfDepth = 2.0F / 16.0F;
        setBoundingBox(0.5F - halfWidth, 0.5F - halfHeight, 0.5F - halfDepth, 0.5F + halfWidth, 0.5F + halfHeight, 0.5F + halfDepth);
    }
}
