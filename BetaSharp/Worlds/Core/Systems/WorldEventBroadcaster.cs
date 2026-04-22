using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Core.Systems;

public class WorldEventBroadcaster(List<IWorldEventListener> eventListeners, IBlockReader reader, World worldContext)
{
    public bool isRemote => worldContext.IsRemote;
    public RuleSet Rules => worldContext.Rules;
    public JavaRandom random => worldContext.Random;

    public void PlaySoundAtEntity(Entity entity, string sound, float volume, float pitch)
    {
        foreach (IWorldEventListener t in eventListeners)
        {
            t.PlaySound(sound, entity.X, entity.Y - entity.StandingEyeHeight, entity.Z, volume, pitch);
        }
    }

    public void PlaySoundAtPos(double x, double y, double z, string sound, float volume, float pitch)
    {
        foreach (IWorldEventListener t in eventListeners)
        {
            t.PlaySound(sound, x, y, z, volume, pitch);
        }
    }

    public void PlayStreamingAtPos(string? music, int x, int y, int z)
    {
        foreach (IWorldEventListener t in eventListeners)
        {
            t.PlayStreaming(music, x, y, z);
        }
    }

    public void AddParticle(string particle, double x, double y, double z, double velocityX, double velocityY, double velocityZ)
    {
        foreach (IWorldEventListener t in eventListeners)
        {
            t.SpawnParticle(particle, x, y, z, velocityX, velocityY, velocityZ);
        }
    }

    public void BlockUpdateEvent(int x, int y, int z)
    {
        foreach (IWorldEventListener t in eventListeners)
        {
            t.BlockUpdate(x, y, z);
        }
    }

    public void WorldEvent(int @event, int x, int y, int z, int data) => WorldEvent(null, @event, x, y, z, data);

    public void WorldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data)
    {
        for (int index = 0; index < eventListeners.Count; ++index)
        {
            eventListeners[index].WorldEvent(player, @event, x, y, z, data);
        }
    }

    public void NotifyNeighbors(int x, int y, int z, int blockId)
    {
        NotifyUpdate(x - 1, y, z, blockId);
        NotifyUpdate(x + 1, y, z, blockId);
        NotifyUpdate(x, y - 1, z, blockId);
        NotifyUpdate(x, y + 1, z, blockId);
        NotifyUpdate(x, y, z - 1, blockId);
        NotifyUpdate(x, y, z + 1, blockId);
    }

    private void NotifyUpdate(int x, int y, int z, int blockId)
    {
        if (isRemote) return;

        int targetBlockId = reader.GetBlockId(x, y, z);
        Block? block = Block.Blocks[targetBlockId];

        if (block == null) return;

        int meta = reader.GetBlockMeta(x, y, z);
        OnTickEvent tickEvent = new(worldContext, x, y, z, meta, blockId);
        block.neighborUpdate(tickEvent);
    }

    public void AddWorldAccess(IWorldEventListener worldAccess) => eventListeners.Add(worldAccess);

    public void RemoveWorldAccess(IWorldEventListener worldAccess) => eventListeners.Remove(worldAccess);

    public void SetBlocksDirty(int x, int z, int minY, int maxY)
    {
        if (minY > maxY)
        {
            (maxY, minY) = (minY, maxY);
        }

        SetBlocksDirty(x, minY, z, x, maxY, z);
    }

    public virtual void PlayNote(int x, int y, int z, int soundType, int pitch)
    {
        int blockId = reader.GetBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockAction(new OnBlockActionEvent(worldContext, soundType, pitch, x, y, z));
        }

        for (int i = 0; i < eventListeners.Count; ++i)
        {
            eventListeners[i].PlayNote(x, y, z, soundType, pitch);
        }
    }

    public void SetBlocksDirty(int x, int y, int z)
    {
        for (int i = 0; i < eventListeners.Count; ++i)
        {
            eventListeners[i].SetBlocksDirty(x, y, z, x, y, z);
        }
    }

    public virtual void EntityEvent(Entity entity, byte @event)
    {
        for (int i = 0; i < eventListeners.Count; ++i)
        {
            eventListeners[i].BroadcastEntityEvent(entity, @event);
        }
    }

    public void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int i = 0; i < eventListeners.Count; ++i)
        {
            eventListeners[i].SetBlocksDirty(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }

    public void UpdateBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        worldContext.Reader.MarkChunkDirty(x, z);
        for (int i = 0; i < eventListeners.Count; ++i)
        {
            eventListeners[i].UpdateBlockEntity(x, y, z, blockEntity);
        }
    }

    public void NotifyAmbientDarknessChanged()
    {
        for (int i = 0; i < eventListeners.Count; ++i)
        {
            eventListeners[i].NotifyAmbientDarknessChanged();
        }
    }
}
