using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds.Core;

public class WorldEventBroadcaster : IBlockWorldContext
{
    private readonly List<IWorldAccess> _eventListeners;

    private readonly World _worldContext;

    public bool PauseTicking = false;
    private readonly IBlockReader _reader;
    public bool isRemote => _worldContext.isRemote;
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
            t.playSound(sound, entity.x, entity.y - entity.standingEyeHeight, entity.z, volume,
                pitch);
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

    void IBlockWorldContext.SpawnEntity(Entity entity)
    {
        if (_worldContext != null)
            _worldContext.Entities.SpawnEntity(entity);
    }

    void IBlockWorldContext.SpawnItemDrop(double x, double y, double z, ItemStack itemStack)
    {
        if (_worldContext == null) return;
        var droppedItem = new EntityItem(_worldContext, x, y, z, itemStack);
        droppedItem.delayBeforeCanPickup = 10;
        _worldContext.Entities.SpawnEntity(droppedItem);
    }

    private void NotifyUpdate(int x, int y, int z, int blockId)
    {
        if (!PauseTicking && !isRemote)
        {
            Block? block = Block.Blocks[_reader.GetBlockId(x, y, z)];
            if (block != null)
            {
                block.neighborUpdate(this, x, y, z, blockId);
            }
        }
    }

    public void AddWorldAccess(IWorldAccess worldAccess) => _eventListeners.Add(worldAccess);

    public void RemoveWorldAccess(IWorldAccess worldAccess) => _eventListeners.Remove(worldAccess);
}
