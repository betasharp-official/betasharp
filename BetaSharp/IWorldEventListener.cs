using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;

namespace BetaSharp;

public interface IWorldEventListener
{
    void blockUpdate(int var1, int var2, int var3);

    void setBlocksDirty(int var1, int var2, int var3, int var4, int var5, int var6);

    void playSound(string var1, double var2, double var4, double var6, float var8, float var9);

    void spawnParticle(string var1, double var2, double var4, double var6, double var8, double var10, double var12);

    void notifyEntityAdded(Entity var1);

    void notifyEntityRemoved(Entity var1);

    void notifyAmbientDarknessChanged();

    void playNote(int x, int y, int z, int soundType, int pitch);

    void playStreaming(string var1, int var2, int var3, int var4);

    void updateBlockEntity(int var1, int var2, int var3, BlockEntity var4);

    void worldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data);

    void broadcastEntityEvent(Entity entity, byte @event);
}