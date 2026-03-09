using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Core.Systems;

public class WorldEventBroadcaster 
{
    private readonly List<IWorldAccess> _eventListeners;
    private readonly World _worldContext;
    private readonly IBlockReader _reader;

    public bool PauseTicking = false;
    public bool isRemote => _worldContext.IsRemote;
    public RuleSet Rules => _worldContext.Rules;
    public JavaRandom random => _worldContext.random;

    public WorldEventBroadcaster(List<IWorldAccess> eventListeners, IBlockReader reader, World worldContext)
    {
        _eventListeners = eventListeners;
        _worldContext = worldContext;
        _reader = reader;
    }

    public void PlaySoundToEntity(Entity entity, string sound, float volume, float pitch)
    {
        foreach (var t in _eventListeners)
        {
            t.playSound(sound, entity.x, entity.y - entity.standingEyeHeight, entity.z, volume, pitch);
        }
    }

    public void PlaySoundAtPos(double x, double y, double z, string sound, float volume, float pitch)
    {
        foreach (var t in _eventListeners)
        {
            t.playSound(sound, x, y, z, volume, pitch);
        }
    }

    public void PlayStreamingAtPos(string? music, int x, int y, int z)
    {
        foreach (var t in _eventListeners)
        {
            t.playStreaming(music, x, y, z);
        }
    }

    public void AddParticle(string particle, double x, double y, double z, double velocityX, double velocityY, double velocityZ)
    {
        foreach (var t in _eventListeners)
        {
            t.spawnParticle(particle, x, y, z, velocityX, velocityY, velocityZ);
        }
    }

    public void BlockUpdateEvent(int x, int y, int z)
    {
        foreach (var t in _eventListeners)
        {
            t.blockUpdate(x, y, z);
        }
    }

    public void WorldEvent(int @event, int x, int y, int z, int data) => WorldEvent(null, @event, x, y, z, data);

    public void WorldEvent(EntityPlayer? player, int @event, int x, int y, int z, int data)
    {
        for (int index = 0; index < _eventListeners.Count; ++index)
        {
            _eventListeners[index].worldEvent(player, @event, x, y, z, data);
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
        if (!PauseTicking && !isRemote)
        {
            int targetBlockId = _reader.GetBlockId(x, y, z);
            Block? block = Block.Blocks[targetBlockId];

            if (block != null)
            {
                int meta = _reader.GetBlockMeta(x, y, z);

                OnTickEvt tickEvent = new OnTickEvt(_worldContext, x, y, z, meta, blockId);

                block.neighborUpdate(tickEvent);
            }
        }
    }

    public void AddWorldAccess(IWorldAccess worldAccess) => _eventListeners.Add(worldAccess);

    public void RemoveWorldAccess(IWorldAccess worldAccess) => _eventListeners.Remove(worldAccess);

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
        int blockId = _reader.GetBlockId(x, y, z);
        if (blockId > 0)
        {
            Block.Blocks[blockId].onBlockAction(new OnBlockActionEvt(_worldContext, soundType, pitch, x, y, z));
        }
    }

    public void SetBlocksDirty(int x, int y, int z)
    {
        for (int i = 0; i < _eventListeners.Count; ++i)
        {
            _eventListeners[i].setBlocksDirty(x, y, z, x, y, z);
        }
    }

    public void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int i = 0; i < _eventListeners.Count; ++i)
        {
            _eventListeners[i].setBlocksDirty(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }
}