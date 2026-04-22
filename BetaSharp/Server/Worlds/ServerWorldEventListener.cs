using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Server.Worlds;

internal class ServerWorldEventListener : IWorldEventListener
{
    private readonly BetaSharpServer server;
    private readonly ServerWorld world;

    public ServerWorldEventListener(BetaSharpServer server, ServerWorld world)
    {
        this.server = server;
        this.world = world;
    }

    public void NotifyEntityAdded(Entity entity)
    {
        server.getEntityTracker(world.Dimension.Id).onEntityAdded(entity);
    }

    public void NotifyEntityRemoved(Entity entity)
    {
        server.getEntityTracker(world.Dimension.Id).onEntityRemoved(entity);
    }

    public void BlockUpdate(int x, int y, int z)
    {
        server.playerManager.markDirty(x, y, z, world.Dimension.Id);
    }

    public void UpdateBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        PlayerManager.updateBlockEntity(x, y, z, blockEntity);
    }

    public void WorldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data)
    {
        server.playerManager.sendToAround(player, x, y, z, 64.0, world.Dimension.Id, WorldEventS2CPacket.Get(@event, x, y, z, data));
        if (player is ServerPlayerEntity serverPlayer && serverPlayer.dimensionId == world.Dimension.Id)
        {
            serverPlayer.NetworkHandler.SendPacket(WorldEventS2CPacket.Get(@event, x, y, z, data));
        }
    }

    public void BroadcastEntityEvent(Entity entity, byte @event)
    {
        EntityStatusS2CPacket packet = EntityStatusS2CPacket.Get(entity.ID, @event);
        server.getEntityTracker(world.Dimension.Id).sendToAround(entity, packet);
    }

    public void PlayNote(int x, int y, int z, int soundType, int pitch)
    {
        server.playerManager.sendToAround(x, y, z, 64.0, world.Dimension.Id, PlayNoteSoundS2CPacket.Get(x, y, z, soundType, pitch));
    }

    public void SpawnParticle(string particle, double x, double y, double z, double velocityX, double velocityY, double velocityZ) { }

    public void PlaySound(string sound, double x, double y, double z, float volume, float pitch) { }

    public void PlayStreaming(string stream, int x, int y, int z) { }

    public void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ) { }

    public void NotifyAmbientDarknessChanged() { }
}
