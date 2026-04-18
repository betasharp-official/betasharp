using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;

namespace BetaSharp;

public interface IWorldEventListener
{
    void BlockUpdate(int x, int y, int z);

    void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ);

    void PlaySound(string soundName, double x, double y, double z, float volume, float pitch);

    void SpawnParticle(string particleName, double x, double y, double z, double velocityX, double velocityY, double velocityZ);

    void NotifyEntityAdded(Entity entity);

    void NotifyEntityRemoved(Entity entity);

    void NotifyAmbientDarknessChanged();

    void PlayNote(int x, int y, int z, int soundType, int pitch);

    void PlayStreaming(string trackName, int x, int y, int z);

    void UpdateBlockEntity(int x, int y, int z, BlockEntity blockEntity);

    void WorldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data);

    void BroadcastEntityEvent(Entity entity, byte @event);
}
