using BetaSharp.Client.Chunks;
using BetaSharp.Client.Network;
using BetaSharp.Entities;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.Worlds;

public class ClientWorld : World
{
    private readonly List<BlockReset> _blockResets = [];
    private readonly ClientNetworkHandler _networkHandler;
    private MultiplayerChunkCache _chunkCache;
    private readonly HashSet<Entity> forcedEntities = [];
    private readonly HashSet<Entity> pendingEntities = [];

    public ClientWorld(ClientNetworkHandler netHandler, long seed, int dimId) : base(new EmptyWorldStorage(), "MpServer", new WorldSettings(seed, WorldType.Default, ""), Dimension.FromId(dimId))
    {
        _networkHandler = netHandler;
        SetSpawnPos(new Vec3i(8, 64, 8));

        StateManager = netHandler.clientPersistentStateManager;
        Entities.OnEntityAdded += HandleEntityAdded;
        Entities.OnEntityRemoved += HandleEntityRemoved;
        Writer.OnBlockChangedWithPrev += HandleBlockChanged;
    }

    public override void Tick()
    {
        SetTime(GetTime() + 1L);

        Environment.UpdateWeatherCycles();

        int ambient = Environment.GetAmbientDarkness(1.0F);
        if (ambient != Environment.AmbientDarkness)
        {
            Environment.AmbientDarkness = ambient;
            Broadcaster.NotifyAmbientDarknessChanged();
        }

        for (int i = 0; i < 10 && pendingEntities.Count > 0; ++i)
        {
            Entity entity = pendingEntities.First();
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
                Writer.OnBlockChangedWithPrev -= HandleBlockChanged;

                Writer.SetBlockWithoutNotifyingNeighbors(blockReset.X, blockReset.Y, blockReset.Z, blockReset.BlockId, blockReset.Meta);
                Broadcaster.BlockUpdateEvent(blockReset.X, blockReset.Y, blockReset.Z);

                Writer.OnBlockChangedWithPrev += HandleBlockChanged;

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

    protected override IChunkSource CreateChunkCache()
    {
        _chunkCache = new MultiplayerChunkCache(this);
        return _chunkCache;
    }

    public override void UpdateSpawnPosition() => SetSpawnPos(new Vec3i(8, 64, 8));

    protected override void ManageChunkUpdatesAndEvents()
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

    private bool SpawnEntity(Entity entity)
    {
        bool spawned = Entities.SpawnEntity(entity);
        forcedEntities.Add(entity);
        if (!spawned)
        {
            pendingEntities.Add(entity);
        }

        return spawned;
    }

    private void Remove(Entity ent)
    {
        Entities.Remove(ent);
        forcedEntities.Remove(ent);
    }

    private void HandleEntityAdded(Entity ent)
    {
        if (pendingEntities.Contains(ent))
        {
            pendingEntities.Remove(ent);
        }
    }

    private void HandleEntityRemoved(Entity ent)
    {
        if (forcedEntities.Contains(ent))
        {
            pendingEntities.Add(ent);
        }
    }

    private void HandleBlockChanged(int x, int y, int z, int previousId, int previousMeta, int newId, int newMeta)
    {
        _blockResets.Add(new BlockReset(this, x, y, z, previousId, previousMeta));
    }

    public void ForceEntity(int networkId, Entity ent)
    {
        Entity? existingEnt = GetEntity(networkId);
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
            forcedEntities.Remove(ent);
            Remove(ent);
        }
        return ent;
    }

    public bool SetBlockWithMetaFromPacket(int minX, int minY, int minZ, int blockId, int meta)
    {
        ClearBlockResets(minX, minY, minZ, minX, minY, minZ);
        if (Writer.SetBlockWithoutNotifyingNeighbors(minX, minY, minZ, blockId, meta))
        {
            BlockUpdate(minX, minY, minZ, blockId);
            return true;
        }

        return false;
    }

    public override void Disconnect() => _networkHandler.sendPacketAndDisconnect(DisconnectPacket.Get("Quitting"));


}
