using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDetectorRail : BlockRail
{
    public BlockDetectorRail(int id, int textureId) : base(id, textureId, true)
    {
        setTickRandomly(true);
    }

    public override int getTickRate()
    {
        return 20;
    }

    public override bool canEmitRedstonePower()
    {
        return true;
    }

    public override void onEntityCollision(OnEntityCollisionEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if ((meta & 8) == 0)
            {
                updatePoweredStatus(@event.World, @event.X, @event.Y, @event.Z, id, meta);
            }
        }
    }

    public override void onTick(OnTickEvent @event)
    {
        if (!@event.World.IsRemote)
        {
            int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
            if ((meta & 8) != 0)
            {
                updatePoweredStatus(@event.World, @event.X, @event.Y, @event.Z, id, meta);
            }
        }
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        return (iBlockReader.GetBlockMeta(x, y, z) & 8) != 0;
    }

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        return (world.GetBlockMeta(x, y, z) & 8) == 0 ? false : side == 1;
    }

    private void updatePoweredStatus(IWorldContext context, int x, int y, int z, int id, int meta)
    {
        bool isPowered = (meta & 8) != 0;
        bool hasMinecart = false;
        float detectionInset = 2.0F / 16.0F;
        List<EntityMinecart> minecartsOnRail = context.Entities.CollectEntitiesOfType<EntityMinecart>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset));
        if (minecartsOnRail.Count > 0)
        {
            hasMinecart = true;
        }

        if (hasMinecart && !isPowered)
        {
            context.Writer.SetBlockMeta(x, y, z, meta | 8);
            context.Broadcaster.NotifyNeighbors(x, y, z, id);
            context.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
            context.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
        }

        if (!hasMinecart && isPowered)
        {
            context.Writer.SetBlockMeta(x, y, z, meta & 7);
            context.Broadcaster.NotifyNeighbors(x, y, z, id);
            context.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
            context.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
        }

        if (hasMinecart)
        {
            context.TickScheduler.ScheduleBlockUpdate(x, y, z, id, getTickRate());
        }
    }
}
