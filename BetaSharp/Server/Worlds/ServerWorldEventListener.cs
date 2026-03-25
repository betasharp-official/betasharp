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

    public void notifyEntityAdded(Entity entity)
    {
        server.getEntityTracker(world.Dimension.Id).onEntityAdded(entity);
    }

    public void notifyEntityRemoved(Entity entity)
    {
        server.getEntityTracker(world.Dimension.Id).onEntityRemoved(entity);
    }

    public void blockUpdate(int x, int y, int z)
    {
        server.playerManager.MarkDirty(x, y, z, world.Dimension.Id);
    }

    public void playStreaming(String stream, int x, int y, int z)
    {
    }

    public void updateBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        server.playerManager.UpdateBlockEntity(x, y, z, blockEntity);
    }

    public void worldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data)
    {
        server.playerManager.sendToAround(player, x, y, z, 64.0, world.Dimension.Id, WorldEventS2CPacket.Get(@event, x, y, z, data));
        if (player is ServerPlayerEntity serverPlayer && serverPlayer.dimensionId == world.Dimension.Id)
        {
            serverPlayer.networkHandler.sendPacket(WorldEventS2CPacket.Get(@event, x, y, z, data));
        }
    }

    public void broadcastEntityEvent(Entity entity, byte @event)
    {
        EntityStatusS2CPacket packet = EntityStatusS2CPacket.Get(entity.id, @event);
        server.getEntityTracker(world.Dimension.Id).sendToAround(entity, packet);
    }

    public void playNote(int x, int y, int z, int soundType, int pitch)
    {
        server.playerManager.SendToAround(x, y, z, 64.0, world.Dimension.Id, PlayNoteSoundS2CPacket.Get(x, y, z, soundType, pitch));
    }

    public void spawnParticle(string particle, double x, double y, double z, double velocityX, double velocityY, double velocityZ) { }
    
    public void playSound(string sound, double x, double y, double z, float volume, float pitch) { }
    
    public void setBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ) { }
    
    public void notifyAmbientDarknessChanged() { }
}