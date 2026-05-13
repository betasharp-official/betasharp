using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockPressurePlate : Block
{
    private const float EdgeInset = 1.0F / 16.0F;
    private const float HalfWidth = 0.5F;
    private const float HalfHeight = 2.0F / 16.0F;
    private const float HalfDepth = 0.5F;

    private const float DetectionInset = 2.0F / 16.0F;

    private readonly PressurePlateActiviationRule _activationRule;

    public BlockPressurePlate(int id, int textureId, PressurePlateActiviationRule rule, Material material) : base(id, textureId, material)
    {
        _activationRule = rule;
        setTickRandomly(true);
        setBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1 / 32f, 1.0F - EdgeInset);
    }

    public override int getTickRate() => 20;

    public override Box? getCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override bool canPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    public override void onPlaced(OnPlacedEvent @event) { }

    public override void neighborUpdate(OnTickEvent @event)
    {
        bool shouldBreak = !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z);

        if (!shouldBreak) return;

        dropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override void onTick(OnTickEvent @event)
    {
        bool wasPressed = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) > 0;
        if (wasPressed)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z, wasPressed);
        }
    }

    public override void onMetadataChange(OnMetadataChangeEvent @event)
    {
        if (!@event.World.IsRemote) return;

        if (@event.Meta == 0)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z, true, false);
        }
    }

    public override void onEntityCollision(OnEntityCollisionEvent @event)
    {
        bool wasPressed = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) > 0;
        if (!wasPressed)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z, wasPressed);
        }
    }

    private void UpdatePlateState(IWorldContext ctx, int x, int y, int z, bool wasPressed)
    {
        bool shouldBePressed = _activationRule switch
        {
            PressurePlateActiviationRule.EVERYTHING => ctx.Entities.CollectEntitiesOfType<Entity>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset)).Any(),
            PressurePlateActiviationRule.MOBS => ctx.Entities.CollectEntitiesOfType<EntityLiving>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset)).Any(),
            PressurePlateActiviationRule.PLAYERS => ctx.Entities.CollectEntitiesOfType<EntityPlayer>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset)).Any(),
            _ => false
        };

        UpdatePlateState(ctx, x, y, z, wasPressed, shouldBePressed);
    }

    private void UpdatePlateState(IWorldContext ctx, int x, int y, int z, bool wasPressed, bool shouldBePressed)
    {
        if (shouldBePressed != wasPressed)
        {
            ctx.Writer.SetBlockMeta(x, y, z, shouldBePressed ? 1 : 0);
            if (!ctx.IsRemote)
            {
                ctx.Broadcaster.NotifyNeighborsFloor(x, y, z, id);
                ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
            }
            else
            {
                ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, shouldBePressed ? 0.6f : 0.5f);
            }
        }

        if (shouldBePressed)
        {
            ctx.TickScheduler.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        }
    }

    public override void onBreak(OnBreakEvent @event)
    {
        int plateState = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (plateState > 0)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, id);
        }

        base.onBreak(@event);
    }

    public override void updateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        bool isPressed = blockReader.GetBlockMeta(x, y, z) == 1;
        if (isPressed)
        {
            setBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1 / 32f, 1.0F - EdgeInset);
        }
        else
        {
            setBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1.0F / 16.0F, 1.0F - EdgeInset);
        }
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => iBlockReader.GetBlockMeta(x, y, z) > 0;

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => world.GetBlockMeta(x, y, z) != 0 && side == 1;

    public override bool canEmitRedstonePower() => true;

    public override void setupRenderBoundingBox() => setBoundingBox(0.5F - HalfWidth, 0.5F - HalfHeight, 0.5F - HalfDepth, 0.5F + HalfWidth, 0.5F + HalfHeight, 0.5F + HalfDepth);

    public override int getPistonBehavior() => 1;
}
