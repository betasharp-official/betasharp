using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockPressurePlate : Block
{
    private readonly PressurePlateActiviationRule activationRule;

    public BlockPressurePlate(int id, int textureId, PressurePlateActiviationRule rule, Material material) : base(id, textureId, material)
    {
        activationRule = rule;
        setTickRandomly(true);
        float edgeInset = 1.0F / 16.0F;
        setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1 / 32f, 1.0F - edgeInset);
    }

    public override int getTickRate() => 20;

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override bool canPlaceAt(CanPlaceAtCtx ctx) => ctx.Level.Reader.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z);

    public override void onPlaced(OnPlacedEvt evt)
    {
    }

    public override void neighborUpdate(OnTickEvt evt)
    {
        bool shouldBreak = false;
        if (!evt.Level.Reader.ShouldSuffocate(evt.X, evt.Y - 1, evt.Z))
        {
            shouldBreak = true;
        }

        if (shouldBreak)
        {
            dropStacks(new OnDropEvt(evt.Level, evt.X, evt.Y, evt.Z, evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z)));
            evt.Level.BlockWriter.SetBlock(evt.X, evt.Y, evt.Z, 0);
        }
    }

    public override void onTick(OnTickEvt evt)
    {
        if (!evt.Level.IsRemote)
        {
            if (evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z) != 0)
            {
                updatePlateState(evt.Level, evt.X, evt.Y, evt.Z);
            }
        }
    }

    public override void onEntityCollision(OnEntityCollisionEvt evt)
    {
        if (!evt.Level.IsRemote)
        {
            if (evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z) != 1)
            {
                updatePlateState(evt.Level, evt.X, evt.Y, evt.Z);
            }
        }
    }

    private void updatePlateState(IWorldContext ctx, int x, int y, int z)
    {
        bool wasPressed = ctx.Reader.GetBlockMeta(x, y, z) == 1;
        bool shouldBePressed = false;
        float detectionInset = 2.0F / 16.0F;
        List<Entity>? entitiesInBox = null;
        if (activationRule == PressurePlateActiviationRule.EVERYTHING)
        {
            entitiesInBox = ctx.Entities.CollectEntitiesOfType<Entity>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset));
        }

        if (activationRule == PressurePlateActiviationRule.MOBS)
        {
            entitiesInBox = ctx.Entities.CollectEntitiesOfType<EntityLiving>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)).Cast<Entity>().ToList();
        }

        if (activationRule == PressurePlateActiviationRule.PLAYERS)
        {
            entitiesInBox = ctx.Entities.CollectEntitiesOfType<EntityPlayer>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset)).Cast<Entity>().ToList();
        }

        if (entitiesInBox?.Count > 0)
        {
            shouldBePressed = true;
        }

        if (shouldBePressed && !wasPressed)
        {
            ctx.BlockWriter.SetBlockMeta(x, y, z, 1);
            ctx.Broadcaster.NotifyNeighbors(x, y, z, id);
            ctx.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
            ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
            ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.6F);
        }

        if (!shouldBePressed && wasPressed)
        {
            ctx.BlockWriter.SetBlockMeta(x, y, z, 0);
            ctx.Broadcaster.NotifyNeighbors(x, y, z, id);
            ctx.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
            ctx.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
            ctx.Broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.1D, z + 0.5D, "random.click", 0.3F, 0.5F);
        }

        if (shouldBePressed)
        {
            ctx.TickScheduler.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        }
    }

    public override void onBreak(OnBreakEvt evt)
    {
        int plateState = evt.Level.Reader.GetBlockMeta(evt.X, evt.Y, evt.Z);
        if (plateState > 0)
        {
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y, evt.Z, id);
            evt.Level.Broadcaster.NotifyNeighbors(evt.X, evt.Y - 1, evt.Z, id);
        }

        base.onBreak(evt);
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        bool isPressed = iBlockReader.GetBlockMeta(x, y, z) == 1;
        float edgeInset = 1.0F / 16.0F;
        if (isPressed)
        {
            setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1 / 32f, 1.0F - edgeInset);
        }
        else
        {
            setBoundingBox(edgeInset, 0.0F, edgeInset, 1.0F - edgeInset, 1.0F / 16.0F, 1.0F - edgeInset);
        }
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => iBlockReader.GetBlockMeta(x, y, z) > 0;

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => world.GetBlockMeta(x, y, z) == 0 ? false : side == 1;

    public override bool canEmitRedstonePower() => true;

    public override void setupRenderBoundingBox()
    {
        float halfWidth = 0.5F;
        float halfHeight = 2.0F / 16.0F;
        float halfDepth = 0.5F;
        setBoundingBox(0.5F - halfWidth, 0.5F - halfHeight, 0.5F - halfDepth, 0.5F + halfWidth, 0.5F + halfHeight, 0.5F + halfDepth);
    }

    public override int getPistonBehavior() => 1;
}
