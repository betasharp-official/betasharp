using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDetectorRail : BlockRail
{
    private const float DetectionInset = 2.0F / 16.0F;
    public BlockDetectorRail(int id, int textureId) : base(id, textureId, true) => SetTickRandomly(true);

    public override int GetTickRate() => 20;

    public override bool CanEmitRedstonePower() => true;

    public override void OnEntityCollision(OnEntityCollisionEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) == 0)
        {
            updatePoweredStatus(@event.World, @event.X, @event.Y, @event.Z, ID, meta);
        }
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote)
        {
            return;
        }

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) != 0)
        {
            updatePoweredStatus(@event.World, @event.X, @event.Y, @event.Z, ID, meta);
        }
    }

    public override bool IsPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => (iBlockReader.GetBlockMeta(x, y, z) & 8) != 0;

    public override bool IsStrongPoweringSide(IBlockReader world, int x, int y, int z, int side) => (world.GetBlockMeta(x, y, z) & 8) == 0 ? false : side == 1;

    private void updatePoweredStatus(IWorldContext context, int x, int y, int z, int id, int meta)
    {
        bool isPowered = (meta & 8) != 0;
        bool hasMinecart = false;

        List<EntityMinecart> minecartsOnRail = context.Entities.CollectEntitiesOfType<EntityMinecart>(new Box(x + DetectionInset, y, z + DetectionInset, x + 1 - DetectionInset, y + 0.25D, z + 1 - DetectionInset));
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
            context.TickScheduler.ScheduleBlockUpdate(x, y, z, id, GetTickRate());
        }
    }
}
