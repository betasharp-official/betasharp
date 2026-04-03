using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;

namespace BetaSharp;

public interface IWorldEventListener
{
    void BlockUpdate(int x, int y, int z);

    void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ);

    void PlaySound(string var1, double var2, double var4, double var6, float var8, float var9);

    void SpawnParticle(string var1, double var2, double var4, double var6, double var8, double var10, double var12);

    void NotifyEntityAdded(Entity var1);

    void NotifyEntityRemoved(Entity var1);

    void NotifyAmbientDarknessChanged();

    void PlayNote(int x, int y, int z, int soundType, int pitch);

    void PlayStreaming(string var1, int var2, int var3, int var4);

    void UpdateBlockEntity(int var1, int var2, int var3, BlockEntity var4);

    void WorldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data);

    void BroadcastEntityEvent(Entity entity, byte @event);
}
