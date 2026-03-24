using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockButton : Block
{
    public BlockButton(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => SetTickRandomly(true);

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override int GetTickRate() => 20;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    private bool IsValidPlacementSide(IBlockReader read, int x, int y, int z, Side side = Side.Down)
    {
        if (side == Side.North)
        {
            return read.ShouldSuffocate(x, y, z + 1);
        }

        return read.ShouldSuffocate(x - 1, y, z) ||
               read.ShouldSuffocate(x + 1, y, z) ||
               read.ShouldSuffocate(x, y, z - 1) ||
               read.ShouldSuffocate(x, y, z + 1);
    }

    public override bool CanPlaceAt(CanPlaceAtContext context) => IsValidPlacementSide(context.World.Reader, context.X, context.Y, context.Z, context.Direction);

    public override void OnPlaced(OnPlacedEvent evt)
    {
        int facing = evt.World.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        int pressedBit = facing & 8;
        facing = evt.Direction switch
        {
            Side.North when evt.World.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z + 1) => 4,
            Side.South when evt.World.Reader.ShouldSuffocate(evt.X, evt.Y, evt.Z - 1) => 3,
            Side.West when evt.World.Reader.ShouldSuffocate(evt.X + 1, evt.Y, evt.Z) => 2,
            Side.East when evt.World.Reader.ShouldSuffocate(evt.X - 1, evt.Y, evt.Z) => 1,
            _ => getPlacementSide(evt.World.Reader, evt.X, evt.Y, evt.Z)
        };

        evt.World.Writer.SetBlockMeta(evt.X, evt.Y, evt.Z, facing + pressedBit);
    }

    private int getPlacementSide(IBlockReader world, int x, int y, int z) =>
        world.ShouldSuffocate(x - 1, y, z) ? 1 : world.ShouldSuffocate(x + 1, y, z) ? 2 : world.ShouldSuffocate(x, y, z - 1) ? 3 : world.ShouldSuffocate(x, y, z + 1) ? 4 : 1;

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!breakIfCannotPlaceAt(@event))
        {
            return;
        }

        int facing = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) & 7;
        bool shouldBreak = (!@event.World.Reader.ShouldSuffocate(@event.X - 1, @event.Y, @event.Z) && facing == 1) ||
                           (!@event.World.Reader.ShouldSuffocate(@event.X + 1, @event.Y, @event.Z) && facing == 2) ||
                           (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z - 1) && facing == 3) ||
                           (!@event.World.Reader.ShouldSuffocate(@event.X, @event.Y, @event.Z + 1) && facing == 4);

        if (!shouldBreak)
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    private bool breakIfCannotPlaceAt(OnTickEvent @event)
    {
        if (!IsValidPlacementSide(@event.World.Reader, @event.X, @event.Y, @event.Z))
        {
            DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
            @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
            return false;
        }

        return true;
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager entities, int x, int y, int z)
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
            SetBoundingBox(0.0F, minY, 0.5F - halfWidth, thickness, maxY, 0.5F + halfWidth);
        }
        else if (facing == 2)
        {
            SetBoundingBox(1.0F - thickness, minY, 0.5F - halfWidth, 1.0F, maxY, 0.5F + halfWidth);
        }
        else if (facing == 3)
        {
            SetBoundingBox(0.5F - halfWidth, minY, 0.0F, 0.5F + halfWidth, maxY, thickness);
        }
        else if (facing == 4)
        {
            SetBoundingBox(0.5F - halfWidth, minY, 1.0F - thickness, 0.5F + halfWidth, maxY, 1.0F);
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
        level.Broadcaster.NotifyNeighbors(x, y, z, Id);
        if (facing == 1)
        {
            level.Broadcaster.NotifyNeighbors(x - 1, y, z, Id);
        }
        else if (facing == 2)
        {
            level.Broadcaster.NotifyNeighbors(x + 1, y, z, Id);
        }
        else if (facing == 3)
        {
            level.Broadcaster.NotifyNeighbors(x, y, z - 1, Id);
        }
        else if (facing == 4)
        {
            level.Broadcaster.NotifyNeighbors(x, y, z + 1, Id);
        }
        else
        {
            level.Broadcaster.NotifyNeighbors(x, y - 1, z, Id);
        }

        level.TickScheduler.ScheduleBlockUpdate(x, y, z, Id, GetTickRate());
        return true;
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent @event) => updateState(@event.World, @event.X, @event.Y, @event.Z);

    public override bool OnUse(OnUseEvent @event) => updateState(@event.World, @event.X, @event.Y, @event.Z);

    public override void OnBreak(OnBreakEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) > 0)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, Id);
            int facing = meta & 7;
            if (facing == 1)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, Id);
            }
            else if (facing == 2)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, Id);
            }
            else if (facing == 3)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, Id);
            }
            else if (facing == 4)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, Id);
            }
            else
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
            }
        }

        base.OnBreak(@event);
    }

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side) => (reader.GetBlockMeta(x, y, z) & 8) > 0;

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side)
    {
        int meta = reader.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int facing = meta & 7;
        return (facing == 5 && side == (int)Side.Up) ||
               (facing == 4 && side == (int)Side.North) ||
               (facing == 3 && side == (int)Side.South) ||
               (facing == 2 && side == (int)Side.West) ||
               (facing == 1 && side == (int)Side.East);
    }

    public override bool CanEmitRedstonePower() => true;

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) != 0)
        {
            @event.World.Writer.SetBlockMeta(@event.X, @event.Y, @event.Z, meta & 7);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, Id);
            int facing = meta & 7;
            if (facing == 1)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, Id);
            }
            else if (facing == 2)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, Id);
            }
            else if (facing == 3)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, Id);
            }
            else if (facing == 4)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, Id);
            }
            else
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
            }

            @event.World.Broadcaster.PlaySoundAtPos(@event.X + 0.5D, @event.Y + 0.5D, @event.Z + 0.5D, "random.click", 0.3F, 0.5F);
            @event.World.Broadcaster.SetBlocksDirty(@event.X, @event.Y, @event.Z);
        }
    }

    public override void SetupRenderBoundingBox()
    {
        float halfWidth = 3.0F / 16.0F;
        float halfHeight = 2.0F / 16.0F;
        float halfDepth = 2.0F / 16.0F;
        SetBoundingBox(0.5F - halfWidth, 0.5F - halfHeight, 0.5F - halfDepth, 0.5F + halfWidth, 0.5F + halfHeight, 0.5F + halfDepth);
    }
}
