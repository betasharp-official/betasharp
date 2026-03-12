using BetaSharp.Client.Chunks;
using BetaSharp.Client.Network;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Worlds;

public class ClientWorld : World
{

    private readonly List<BlockReset> _blockResets = [];
    private readonly ClientNetworkHandler _networkHandler;
    private MultiplayerChunkCache _chunkCache;
    private readonly Dictionary<int, Entity> entitiesByNetworkId = new();
    private readonly HashSet<Entity> forcedEntities = [];
    private readonly HashSet<Entity> pendingEntities = [];

    public ClientWorld(ClientNetworkHandler netHandler, long seed, int dimId) : base(new EmptyWorldStorage(), "MpServer", new WorldSettings(seed, WorldType.Default), Dimension.FromId(dimId))
    {
        _networkHandler = netHandler;
        setSpawnPos(new Vec3i(8, 64, 8));
        persistentStateManager = netHandler.clientPersistentStateManager;
    }

    public override void Tick()
    {
        setTime(getTime() + 1L);
        int ambient = getAmbientDarkness(1.0F);

        if (ambient != ambientDarkness)
        {
            ambientDarkness = ambient;
            for (int j = 0; j < EventListeners.Count; ++j)
            {
                EventListeners[j].notifyAmbientDarknessChanged();
            }
        }

        for (int i = 0; i < 10 && pendingEntities.Count > 0; ++i)
        {
            Entity entity = pendingEntities.First();
            if (!entities.Contains(entity))
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

    protected override void ManageChunkUpdatesAndEvents() { }

    public override void ScheduleBlockUpdate(int x, int y, int z, int blockId, int delay) { }

    protected override void ProcessScheduledTicks(bool flush) { }

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

    public override bool SpawnEntity(Entity entity)
    {
        bool spawned = base.SpawnEntity(entity);
        forcedEntities.Add(entity);
        if (!spawned)
        {
            pendingEntities.Add(entity);
        }

        return spawned;
    }

    public override void Remove(Entity ent)
    {
        base.Remove(ent);
        forcedEntities.Remove(ent);
    }

    protected override void NotifyEntityAdded(Entity ent)
    {
        base.NotifyEntityAdded(ent);
        if (pendingEntities.Contains(ent))
        {
            pendingEntities.Remove(ent);
        }

    }

    protected override void NotifyEntityRemoved(Entity ent)
    {
        base.NotifyEntityRemoved(ent);
        if (forcedEntities.Contains(ent))
        {
            pendingEntities.Add(ent);
        }

    }

    public void ForceEntity(int networkId, Entity ent)
    {
        Entity existingEnt = GetEntity(networkId);
        if (existingEnt != null)
        {
            Remove(existingEnt);
        }

        forcedEntities.Add(ent);
        ent.id = networkId;

        if (!SpawnEntity(ent))
        {
            pendingEntities.Add(ent);
        }

        entitiesByNetworkId[networkId] = ent;
    }

    public Entity GetEntity(int networkId)
    {
        return entitiesByNetworkId.GetValueOrDefault(networkId);
    }

    public Entity RemoveEntityFromWorld(int networkId)
    {
        if (entitiesByNetworkId.Remove(networkId, out Entity ent))
        {
            forcedEntities.Remove(ent);
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

    public override void Disconnect() => _networkHandler.sendPacketAndDisconnect(DisconnectPacket.Get("Quitting"));


    protected override void UpdateWeatherCycles()
    {
        if (dimension.HasCeiling) return;

        if (TicksSinceLightning > 0) --TicksSinceLightning;

        PrevRainingStrength = RainingStrength;
        RainingStrength = Math.Clamp(RainingStrength + (Properties.IsRaining ? 0.01f : -0.01f), 0.0f, 1.0f);

        PrevThunderingStrength = ThunderingStrength;
        ThunderingStrength = Math.Clamp(ThunderingStrength + (Properties.IsThundering ? 0.01f : -0.01f), 0.0f, 1.0f);
    }
}
