using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Worlds;

namespace BetaSharp.Server.Worlds;

internal class ServerWorldEventListener : IWorldAccess
{
    private readonly MinecraftServer server;
    private readonly ServerWorld world;

    public ServerWorldEventListener(MinecraftServer server, ServerWorld world)
    {
        this.server = server;
        this.world = world;
    }

    public void addParticle(string particle, double x, double y, double z, double velocityX, double velocityY, double velocityZ)
    {
    }

    public void notifyEntityAdded(Entity entity)
    {
        server.GetEntityTracker(world.dimension.Id).OnEntityAdded(entity);
    }

    public void notifyEntityRemoved(Entity entity)
    {
        server.GetEntityTracker(world.dimension.Id).OnEntityRemoved(entity);
    }

    public void playSound(string sound, double x, double y, double z, float volume, float pitch)
    {
    }

    public void setBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
    }

    public void notifyAmbientDarknessChanged()
    {
    }

    public void blockUpdate(int x, int y, int z)
    {
        server.PlayerManager.MarkDirty(x, y, z, world.dimension.Id);
    }

    public void playStreaming(String stream, int x, int y, int z)
    {
    }

    public void UpdateBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        server.PlayerManager.UpdateBlockEntity(x, y, z, blockEntity);
    }

    public void worldEvent(EntityPlayer player, int @event, int x, int y, int z, int data)
    {
        server.PlayerManager.SendToAround(player, x, y, z, 64.0, world.dimension.Id, new WorldEventS2CPacket(@event, x, y, z, data));
    }

    public void spawnParticle(string var1, double var2, double var4, double var6, double var8, double var10, double var12)
    {
    }
}
