using BetaSharp.Blocks.Materials;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockButton : Block
{
    private const float minY = 6.0F / 16.0F;
    private const float maxY = 10.0F / 16.0F;
    private const float halfWidth = 3.0F / 16.0F;
    private const float thickness = 2.0F / 16.0F;
    private const float pressedThickness = 1.0F / 16.0F;
    public BlockButton(int id, int textureId) : base(id, textureId, Material.PistonBreakable) => SetTickRandomly(true);

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override int GetTickRate() => 20;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    private static bool IsValidPlacementSide(IBlockReader read, int x, int y, int z, Side side = Side.Down)
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
            Side.Up or Side.Down => GetPlacementSide(evt.World.Reader, evt.X, evt.Y, evt.Z)
        };

        evt.World.Writer.SetBlockMeta(evt.X, evt.Y, evt.Z, facing + pressedBit);
    }

    private static int GetPlacementSide(IBlockReader world, int x, int y, int z) =>
        world.ShouldSuffocate(x - 1, y, z) ? 1 : world.ShouldSuffocate(x + 1, y, z) ? 2 : world.ShouldSuffocate(x, y, z - 1) ? 3 : world.ShouldSuffocate(x, y, z + 1) ? 4 : 1;

    public override void NeighborUpdate(OnTickEvent @event)
    {
        if (!BreakIfCannotPlaceAt(@event))
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

    private bool BreakIfCannotPlaceAt(OnTickEvent @event)
    {
        if (IsValidPlacementSide(@event.World.Reader, @event.X, @event.Y, @event.Z))
        {
            return true;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
        return false;
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager entities, int x, int y, int z)
    {
        int meta = blockReader.GetBlockMeta(x, y, z);
        Side facing = (meta & 7).ToSide();
        bool isPressed = (meta & 8) > 0;

        float height = thickness;
        if (isPressed)
        {
            height = pressedThickness;
        }

        switch (facing)
        {
            case Side.Up:
                SetBoundingBox(0.0F, minY, 0.5F - halfWidth, height, maxY, 0.5F + halfWidth);
                break;
            case Side.North:
                SetBoundingBox(1.0F - height, minY, 0.5F - halfWidth, 1.0F, maxY, 0.5F + halfWidth);
                break;
            case Side.South:
                SetBoundingBox(0.5F - halfWidth, minY, 0.0F, 0.5F + halfWidth, maxY, height);
                break;
            case Side.West:
                SetBoundingBox(0.5F - halfWidth, minY, 1.0F - height, 0.5F + halfWidth, maxY, 1.0F);
                break;
        }
    }


    private bool UpdateState(IWorldContext level, int x, int y, int z)
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
        level.Broadcaster.NotifyNeighbors(x, y, z, ID);
        switch (facing)
        {
            case 1:
                level.Broadcaster.NotifyNeighbors(x - 1, y, z, ID);
                break;
            case 2:
                level.Broadcaster.NotifyNeighbors(x + 1, y, z, ID);
                break;
            case 3:
                level.Broadcaster.NotifyNeighbors(x, y, z - 1, ID);
                break;
            case 4:
                level.Broadcaster.NotifyNeighbors(x, y, z + 1, ID);
                break;
            default:
                level.Broadcaster.NotifyNeighbors(x, y - 1, z, ID);
                break;
        }

        level.TickScheduler.ScheduleBlockUpdate(x, y, z, ID, GetTickRate());
        return true;
    }

    public override void OnBlockBreakStart(OnBlockBreakStartEvent @event) => UpdateState(@event.World, @event.X, @event.Y, @event.Z);

    public override bool OnUse(OnUseEvent @event) => UpdateState(@event.World, @event.X, @event.Y, @event.Z);

    public override void OnBreak(OnBreakEvent @event)
    {
        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) > 0)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, ID);
            int facing = meta & 7;
            switch (facing)
            {
                case 1:
                    @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, ID);
                    break;
                case 2:
                    @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, ID);
                    break;
                case 3:
                    @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, ID);
                    break;
                case 4:
                    @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, ID);
                    break;
                default:
                    @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, ID);
                    break;
            }
        }

        base.OnBreak(@event);
    }

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side) => (reader.GetBlockMeta(x, y, z) & 8) > 0;

    public override bool IsStrongPoweringSide(IBlockReader read, int x, int y, int z, int side)
    {
        int meta = read.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int facing = meta & 7;
        return (facing == 5 && side == 1) ||
               (facing == 4 && side == 2) ||
               (facing == 3 && side == 3) ||
               (facing == 2 && side == 4) ||
               (facing == 1 && side == 5);
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
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, ID);
            int facing = meta & 7;
            if (facing == 1)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X - 1, @event.Y, @event.Z, ID);
            }
            else if (facing == 2)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X + 1, @event.Y, @event.Z, ID);
            }
            else if (facing == 3)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z - 1, ID);
            }
            else if (facing == 4)
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z + 1, ID);
            }
            else
            {
                @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, ID);
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
