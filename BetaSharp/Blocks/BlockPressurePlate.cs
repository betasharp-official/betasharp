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
        SetTickRandomly(true);
        SetBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1 / 32f, 1.0F - EdgeInset);
    }

    public override int GetTickRate() => 20;

    public override Box? GetCollisionShape(IBlockReader world, EntityManager entities, int x, int y, int z) => null;

    public override bool IsOpaque() => false;

    public override bool IsFullCube() => false;

    public override bool CanPlaceAt(CanPlaceAtContext context) => context.World.Reader.ShouldSuffocate(context.X, context.Y - 1, context.Z);

    public override void OnPlaced(OnPlacedEvent @event)
    {
    }

    public override void NeighborUpdate(OnTickEvent @event)
    {
        bool shouldBreak = !@event.World.Reader.ShouldSuffocate(@event.X, @event.Y - 1, @event.Z);

        if (!shouldBreak)
        {
            return;
        }

        DropStacks(new OnDropEvent(@event.World, @event.X, @event.Y, @event.Z, @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z)));
        @event.World.Writer.SetBlock(@event.X, @event.Y, @event.Z, 0);
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) != 0)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    public override void OnEntityCollision(OnEntityCollisionEvent @event)
    {
        if (@event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z) != 1)
        {
            UpdatePlateState(@event.World, @event.X, @event.Y, @event.Z);
        }
    }

    private void UpdatePlateState(IWorldContext ctx, int x, int y, int z)
    {
        bool wasPressed = ctx.Reader.GetBlockMeta(x, y, z) == 1;
        bool shouldBePressed = false;

        List<Entity>? entitiesInBox = _activationRule switch
        {
            PressurePlateActiviationRule.EVERYTHING => ctx.Entities.CollectEntitiesOfType<Entity>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset)),
            PressurePlateActiviationRule.MOBS => ctx.Entities.CollectEntitiesOfType<EntityLiving>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset)).Cast<Entity>().ToList(),
            PressurePlateActiviationRule.PLAYERS => ctx.Entities.CollectEntitiesOfType<EntityPlayer>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset)).Cast<Entity>().ToList(),
            _ => null
        };

        if (entitiesInBox?.Count > 0)
        {
            shouldBePressed = true;
        }

        switch (shouldBePressed)
        {
            case true when !wasPressed:
                if (!ctx.IsRemote)
                {
                    ctx.Writer.SetBlockMeta(x, y, z, 1);
                    ctx.Broadcaster.NotifyNeighbors(x, y, z, ID);
                    ctx.Broadcaster.NotifyNeighbors(x, y - 1, z, ID);
                    ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
                }
                else
                {
                    ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.6F);
                }

                break;

            case false when wasPressed:
                if (!ctx.IsRemote)
                {
                    ctx.Writer.SetBlockMeta(x, y, z, 0);
                    ctx.Broadcaster.NotifyNeighbors(x, y, z, ID);
                    ctx.Broadcaster.NotifyNeighbors(x, y - 1, z, ID);
                    ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
                }
                else
                {
                    ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.5F);
                }

                break;
        }

        if (shouldBePressed && !ctx.IsRemote)
        {
            ctx.TickScheduler.ScheduleBlockUpdate(x, y, z, ID, GetTickRate());
        }
    }

    public override void OnBreak(OnBreakEvent @event)
    {
        int plateState = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if (plateState > 0)
        {
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y, @event.Z, ID);
            @event.World.Broadcaster.NotifyNeighbors(@event.X, @event.Y - 1, @event.Z, ID);
        }

        base.OnBreak(@event);
    }

    public override void UpdateBoundingBox(IBlockReader blockReader, EntityManager? entities, int x, int y, int z)
    {
        bool isPressed = blockReader.GetBlockMeta(x, y, z) == 1;
        if (isPressed)
        {
            SetBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1 / 32f, 1.0F - EdgeInset);
        }
        else
        {
            SetBoundingBox(EdgeInset, 0.0F, EdgeInset, 1.0F - EdgeInset, 1.0F / 16.0F, 1.0F - EdgeInset);
        }
    }

    public override bool IsPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => iBlockReader.GetBlockMeta(x, y, z) > 0;

    public override bool IsStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => world.GetBlockMeta(x, y, z) != 0 && side == 1;

    public override bool CanEmitRedstonePower() => true;

    public override void SetupRenderBoundingBox() => SetBoundingBox(0.5F - HalfWidth, 0.5F - HalfHeight, 0.5F - HalfDepth, 0.5F + HalfWidth, 0.5F + HalfHeight, 0.5F + HalfDepth);

    public override PistonBehavior GetPistonBehavior() => PistonBehavior.Break;
}
