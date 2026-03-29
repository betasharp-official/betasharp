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

    private readonly PressurePlateActiviationRule _activationRule;
    public override int TickRate => 20;
    public override bool IsOpaque => false;
    public override bool IsFullCube => false;
    public override PistonBehavior PistonBehavior => PistonBehavior.Destroy;

    public BlockPressurePlate(int id, int textureId, PressurePlateActiviationRule rule, Material material) : base(id, textureId, material)
    {
        _activationRule = rule;
        SetTickRandomly(true);
        SetBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1 / 32f, 1.0F - EdgeInset);
    }

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    public override void OnPlaced(OnPlacedEvent @event)
    {
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        bool shouldBreak = !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z);

        if (!shouldBreak) return;

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) != 0)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    public override void OnEntityCollision(OnEntityCollisionEvent @event)
    {
        if (@event.World.IsRemote) return;

        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) != 1)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private void UpdatePlateState(IWorldContext ctx, int x, int y, int z)
    {
        const float detectionInset = 2.0F / 16.0F;
        bool wasPressed = ctx.Reader.GetBlockMeta(x, y, z) == 1;

        List<Entity>? entitiesInBox = _activationRule switch
        {
            PressurePlateActiviationRule.EVERYTHING => ctx.Entities.CollectEntitiesOfType<Entity>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)),
            PressurePlateActiviationRule.MOBS => ctx.Entities.CollectEntitiesOfType<EntityLiving>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)).Cast<Entity>().ToList(),
            PressurePlateActiviationRule.PLAYERS => ctx.Entities.CollectEntitiesOfType<EntityPlayer>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)).Cast<Entity>().ToList(),
            _ => null
        };

        bool shouldBePressed = entitiesInBox?.Count > 0;

        switch (shouldBePressed)
        {
            case true when !wasPressed:
                ctx.Writer.SetBlockMeta(x, y, z, 1);
                ctx.Broadcaster.NotifyNeighbors(x, y, z, Id);
                ctx.Broadcaster.NotifyNeighbors(x, y - 1, z, Id);
                ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
                ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.6F);
                break;
            case false when wasPressed:
                ctx.Writer.SetBlockMeta(x, y, z, 0);
                ctx.Broadcaster.NotifyNeighbors(x, y, z, Id);
                ctx.Broadcaster.NotifyNeighbors(x, y - 1, z, Id);
                ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
                ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.5F);
                break;
        }

        if (shouldBePressed)
        {
            ctx.TickScheduler.ScheduleBlockUpdate(x, y, z, Id, TickRate);
        }
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        int plateState = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (plateState > 0)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, Id);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, Id);
        }

        base.OnBreak(@event);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        const float edgeInset = 1.0F / 16.0F;
        bool isPressed = blockReader.GetBlockMeta(x, y, z) == 1;
        if (isPressed)
        {
            SetBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1 / 32f, 1.0F - edgeInset);
        }
        else
        {
            SetBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1.0F / 16.0F, 1.0F - edgeInset);
        }
    }

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side) => reader.GetBlockMeta(x, y, z) > 0;

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side) => reader.GetBlockMeta(x, y, z) != 0 && side == Side.Up.ToInt();

    public override bool CanEmitRedstonePower => true;

    public override void SetupRenderBoundingBox()
    {
        SetBoundingBox(0.5F - HalfWidth, 0.5F - HalfHeight, 0.5F - HalfDepth, 0.5F + HalfWidth, 0.5F + HalfHeight, 0.5F + HalfDepth);
    }
}
