using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

internal class BlockDetectorRail : BlockRail
{
    public BlockDetectorRail(int id, int textureId) : base(id, textureId, true) => SetTickRandomly(true);

    public override int GetTickRate() => 20;

    public override bool CanEmitRedstonePower() => true;

    public override void OnEntityCollision(OnEntityCollisionEvent @event)
    {
        if (@event.World.IsRemote) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) == 0)
        {
            updatePoweredStatus(@event.World, @event.X, @event.Y, @event.Z, Id, meta);
        }
    }

    public override void OnTick(OnTickEvent @event)
    {
        if (@event.World.IsRemote) return;

        int meta = @event.World.Reader.GetBlockMeta(@event.X, @event.Y, @event.Z);
        if ((meta & 8) != 0)
        {
            updatePoweredStatus(@event.World, @event.X, @event.Y, @event.Z, Id, meta);
        }
    }

    public override bool IsPoweringSide(IBlockReader reader, int x, int y, int z, int side) => (reader.GetBlockMeta(x, y, z) & 8) != 0;

    public override bool IsStrongPoweringSide(IBlockReader reader, int x, int y, int z, int side) => (reader.GetBlockMeta(x, y, z) & 8) != 0 && side == (int)Side.Up;

    private void updatePoweredStatus(IWorldContext context, int x, int y, int z, int id, int meta)
    {
        const float detectionInset = 2.0F / 16.0F;
        List<EntityMinecart> minecartsOnRail = context.Entities.CollectEntitiesOfType<EntityMinecart>(new Box(x + detectionInset, y, z + detectionInset, x + 1 - detectionInset, y + 0.25D, z + 1 - detectionInset));

        bool isPowered = (meta & 8) != 0;
        bool hasMinecart = minecartsOnRail.Count > 0;
        switch (hasMinecart)
        {
            case true when !isPowered:
                context.Writer.SetBlockMeta(x, y, z, meta | 8);
                context.Broadcaster.NotifyNeighbors(x, y, z, id);
                context.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
                context.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
                break;
            case false when isPowered:
                context.Writer.SetBlockMeta(x, y, z, meta & 7);
                context.Broadcaster.NotifyNeighbors(x, y, z, id);
                context.Broadcaster.NotifyNeighbors(x, y - 1, z, id);
                context.Broadcaster.SetBlocksDirty(x, y, z, x, y, z);
                break;
        }

        if (hasMinecart)
        {
            context.TickScheduler.ScheduleBlockUpdate(x, y, z, id, GetTickRate());
        }
    }
}
