using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;

namespace BetaSharp;

public interface IWorldAccess
{
    void blockUpdate(int x, int y, int z);

    void setBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ);

    void playSound(string soundName, double x, double y, double z, float volume, float pitch);

    void spawnParticle(string particleName, double particleX, double particleY, double particleZ, double velocityX, double velocityY, double velocityZ);

    void notifyEntityAdded(Entity entity);

    void notifyEntityRemoved(Entity entity);

    void notifyAmbientDarknessChanged();

    void playStreaming(string soundName, int x, int y, int z);

    void updateBlockEntity(int x, int y, int z, BlockEntity blockEntity);

    void worldEvent(EntityPlayer player, int eventType, int x, int y, int z, int data);
}