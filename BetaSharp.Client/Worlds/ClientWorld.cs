using BetaSharp.Client.Chunks;
using BetaSharp.Client.Network;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Worlds;

public class ClientWorld : World
{
    private readonly List<BlockReset> _blockResets = [];
    private readonly ClientNetworkHandler _networkHandler;
    private MultiplayerChunkCache _chunkCache;
    private readonly HashSet<Entity> _forcedEntities = [];
    private readonly HashSet<Entity> _pendingEntities = [];

    public ClientWorld(ClientNetworkHandler netHandler, long seed, int dimId) : base(new EmptyWorldStorage(), "MpServer", Dimension.FromId(dimId), seed)
    {
        _networkHandler = netHandler;
        setSpawnPos(new Vec3i(8, 64, 8));
        PersistentStateManager = netHandler.clientPersistentStateManager;

        Entities.OnEntityAdded += HandleEntityAdded;
        Entities.OnEntityRemoved += HandleEntityRemoved;
    }

    public override void Tick()
    {
        setTime(getTime() + 1L);
        Environment.UpdateWeatherCycles();
        int ambient = Environment.GetAmbientDarkness(1.0F);

        if (ambient != ambientDarkness)
        {
            ambientDarkness = ambient;
            for (int j = 0; j < EventListeners.Count; ++j)
            {
                EventListeners[j].notifyAmbientDarknessChanged();
            }
        }

        for (int i = 0; i < 10 && _pendingEntities.Count > 0; ++i)
        {
            Entity entity = _pendingEntities.First();
            if (!Entities.Entities.Contains(entity))
            {
                SpawnEntity(entity);
            }
        }

        _networkHandler.tick();

        for (int i = 0; i < _blockResets.Count; ++i)
        {
            BlockReset blockReset = _blockResets[i];
            if (--blockReset.Delay == 0)
            {
                base.SetBlockWithoutNotifyingNeighbors(blockReset.X, blockReset.Y, blockReset.Z, blockReset.BlockId, blockReset.Meta);
                blockUpdateEvent(blockReset.X, blockReset.Y, blockReset.Z);
                _blockResets.RemoveAt(i--);
            }
        }
    }

    public void ClearBlockResets(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int i = 0; i < _blockResets.Count; ++i)
        {
            BlockReset br = _blockResets[i];
            if (br.X >= minX && br.Y >= minY && br.Z >= minZ &&
                br.X <= maxX && br.Y <= maxY && br.Z <= maxZ)
            {
                _blockResets.RemoveAt(i--);
            }
        }
    }

    protected override ChunkSource CreateChunkCache()
    {
        _chunkCache = new MultiplayerChunkCache(this);
        return _chunkCache;
    }

    public override void UpdateSpawnPosition() => setSpawnPos(new Vec3i(8, 64, 8));

    protected override void ManageChunkUpdatesAndEvents()
    {
    }

    public override void ScheduleBlockUpdate(int x, int y, int z, int blockId, int delay)
    {
    }

    protected override void ProcessScheduledTicks(bool flush)
    {
    }

    public void UpdateChunk(int chunkX, int chunkZ, bool load)
    {
        if (load)
        {
            _chunkCache.LoadChunk(chunkX, chunkZ);
        }
        else
        {
            _chunkCache.UnloadChunk(chunkX, chunkZ);
        }

        if (!load)
        {
            setBlocksDirty(chunkX * 16, 0, chunkZ * 16, chunkX * 16 + 15, 128, chunkZ * 16 + 15);
        }
    }

    private bool SpawnEntity(Entity entity) // Issue here
    {
        bool spawned = Entities.SpawnEntity(entity);
        _forcedEntities.Add(entity);
        if (!spawned)
        {
            _pendingEntities.Add(entity);
        }

        return spawned;
    }

    private void Remove(Entity ent)
    {
        Entities.Remove(ent);
        _forcedEntities.Remove(ent);
    }

    private void HandleEntityAdded(Entity ent)
    {
        if (_pendingEntities.Contains(ent))
        {
            _pendingEntities.Remove(ent);
        }
    }

    private void HandleEntityRemoved(Entity ent)
    {
        if (_forcedEntities.Contains(ent))
        {
            _pendingEntities.Add(ent);
        }
    }

    public void ForceEntity(int networkId, Entity ent)
    {
        Entity? existingEnt = GetEntity(networkId);
        if (existingEnt != null)
        {
            Remove(existingEnt);
        }

        _forcedEntities.Add(ent);
        ent.id = networkId;

        if (!SpawnEntity(ent))
        {
            _pendingEntities.Add(ent);
        }
    }

    public Entity? GetEntity(int networkId)
    {
        return Entities.GetEntityByID(networkId);
    }

    public Entity? RemoveEntityFromWorld(int networkId)
    {
        Entity? ent = GetEntity(networkId);
        if (ent != null)
        {
            _forcedEntities.Remove(ent);
            Remove(ent);
        }
        return ent;
    }

    public override bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta)
    {
        int blockId = getBlockId(x, y, z);
        int previousMeta = getBlockMeta(x, y, z);
        if (base.SetBlockMetaWithoutNotifyingNeighbors(x, y, z, meta))
        {
            _blockResets.Add(new BlockReset(this, x, y, z, blockId, previousMeta));
            return true;
        }

        return false;
    }

    public override bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta)
    {
        int previousBlockId = getBlockId(x, y, z);
        int previousMeta = getBlockMeta(x, y, z);
        if (base.SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta))
        {
            _blockResets.Add(new BlockReset(this, x, y, z, previousBlockId, previousMeta));
            return true;
        }

        return false;
    }

    public override bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId)
    {
        int previousBlockId = getBlockId(x, y, z);
        int previousMeta = getBlockMeta(x, y, z);
        if (base.SetBlockWithoutNotifyingNeighbors(x, y, z, blockId))
        {
            _blockResets.Add(new BlockReset(this, x, y, z, previousBlockId, previousMeta));
            return true;
        }

        return false;
    }

    public bool SetBlockWithMetaFromPacket(int minX, int minY, int minZ, int blockId, int meta)
    {
        ClearBlockResets(minX, minY, minZ, minX, minY, minZ);
        if (base.SetBlockWithoutNotifyingNeighbors(minX, minY, minZ, blockId, meta))
        {
            blockUpdate(minX, minY, minZ, blockId);
            return true;
        }

        return false;
    }

    public override void Disconnect() => _networkHandler.sendPacketAndDisconnect(new DisconnectPacket("Quitting"));
}
