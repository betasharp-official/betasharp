using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Profiling;
using BetaSharp.Rules;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds.Core.Systems;

public class EntityManager
{
    [ThreadStatic] private static List<Entity>? _tempCollisionEntities;
    [ThreadStatic] private static List<Box>? _tempCollisionBoxes;
    [ThreadStatic] private static List<Entity>? _tempCollisionEntitiesResult;
    private readonly List<BlockEntity> _blockEntityUpdateQueue = [];

    private readonly WorldBlockReader _blocks;
    private readonly Dictionary<int, Entity> _entitiesById = new();
    private readonly List<Entity> _entitiesToUnload = [];
    private readonly BlockHost _host;
    private readonly RuleSet _rules;
    private bool _processingDeferred;

    public List<BlockEntity> BlockEntities = [];
    public List<Entity> Entities = [];
    public List<Entity> GlobalEntities = [];

    public List<EntityPlayer> Players = [];

    public EntityManager(WorldBlockReader blocks, RuleSet rules, BlockHost host)
    {
        _blocks = blocks;
        _rules = rules;
        _host = host;
    }

    public bool AllPlayersSleeping { get; private set; }

    public event Action<Entity>? OnEntityAdded;
    public event Action<Entity>? OnEntityRemoved;
    public event Action<Entity>? OnGlobalEntityAdded;
    public event Func<Entity, bool>? OnEntityUpdating;
    public event Action<int, int, int>? OnBlockUpdateRequired;

    public bool SpawnGlobalEntity(Entity entity)
    {
        GlobalEntities.Add(entity);
        OnGlobalEntityAdded?.Invoke(entity);
        return true;
    }

    public void UpdateEntityLists()
    {
        // 1. Clean up unloaded entities
        foreach (Entity entity in _entitiesToUnload)
        {
            Entities.Remove(entity);
        }

        for (int i = 0; i < _entitiesToUnload.Count; ++i)
        {
            Entity entity = _entitiesToUnload[i];
            int chunkX = entity.chunkX;
            int chunkZ = entity.chunkZ;

            if (entity.isPersistent && _host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
            {
                _host.GetChunk(chunkX, chunkZ).RemoveEntity(entity);
            }

            if (_entitiesById.TryGetValue(entity.id, out Entity? current) && ReferenceEquals(current, entity))
                _entitiesById.Remove(entity.id);
            NotifyEntityRemoved(entity);
        }

        _entitiesToUnload.Clear();

        // 2. Process active entities and clean up dead ones
        for (int i = 0; i < Entities.Count; ++i)
        {
            Entity entity = Entities[i];

            if (entity.vehicle != null)
            {
                if (!entity.vehicle.dead && Equals(entity.vehicle.passenger, entity))
                {
                    continue;
                }

                entity.vehicle.passenger = null;
                entity.vehicle = null;
            }

            if (entity.dead)
            {
                int chunkX = entity.chunkX;
                int chunkZ = entity.chunkZ;

                if (entity.isPersistent && _host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
                {
                    _host.GetChunk(chunkX, chunkZ).RemoveEntity(entity);
                }

                Entities.RemoveAt(i--);
                if (_entitiesById.TryGetValue(entity.id, out Entity? current2) && ReferenceEquals(current2, entity))
                    _entitiesById.Remove(entity.id);
                NotifyEntityRemoved(entity);
            }
        }
    }

    public bool SpawnEntity(Entity entity)
    {
        int chunkX = MathHelper.Floor(entity.x / 16.0D);
        int chunkZ = MathHelper.Floor(entity.z / 16.0D);
        bool isPlayer = entity is EntityPlayer;

        if (!isPlayer && !_host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
        {
            return false;
        }


        if (entity is EntityPlayer player)
        {
            Players.Add(player);
        }


        _host.GetChunk(chunkX, chunkZ).AddEntity(entity);
        Entities.Add(entity);
        _entitiesById[entity.id] = entity;

        NotifyEntityAdded(entity);
        return true;
    }

    public void Remove(Entity entity)
    {
        if (entity.passenger != null)
        {
            entity.passenger.setVehicle(null);
        }

        if (entity.vehicle != null)
        {
            entity.setVehicle(null);
        }

        entity.markDead();
        if (entity is EntityPlayer player)
        {
            Players.Remove(player);
        }
    }

    public void ServerRemove(Entity entity)
    {
        entity.markDead();
        if (entity is EntityPlayer player)
        {
            Players.Remove(player);
        }


        int chunkX = entity.chunkX;
        int chunkZ = entity.chunkZ;
        if (entity.isPersistent && _host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
        {
            _host.GetChunk(chunkX, chunkZ).RemoveEntity(entity);
        }

        Entities.Remove(entity);
        _entitiesById.Remove(entity.id);
        NotifyEntityRemoved(entity);
    }

    public bool AreAllPlayersAsleep()
    {
        if (Players.Count == 0)
        {
            return false;
        }

        return Players.All(p => p.isPlayerFullyAsleep());
    }

    public void WakeAllPlayers()
    {
        foreach (EntityPlayer player in Players.Where(p => p.isSleeping()))
        {
            player.wakeUp(false, false, true);
        }
    }

    private void NotifyEntityAdded(Entity entity) => OnEntityAdded?.Invoke(entity);

    private void NotifyEntityRemoved(Entity entity) => OnEntityRemoved?.Invoke(entity);

    public List<BlockEntity> GetBlockEntities(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        List<BlockEntity> blockEntInArea = [];

        for (int var8 = 0; var8 < BlockEntities.Count; var8++)
        {
            BlockEntity blockEnt = BlockEntities[var8];
            if (blockEnt.X >= minX && blockEnt.Y >= minY && blockEnt.Z >= minZ && blockEnt.X < maxX && blockEnt.Y < maxY && blockEnt.Z < maxZ)
            {
                blockEntInArea.Add(blockEnt);
            }
        }

        return blockEntInArea;
    }

    public void TickEntities()
    {
        Profiler.Start("updateEntites.updateWeatherEffects");
        for (int i = 0; i < GlobalEntities.Count; ++i)
        {
            Entity globalEntity = GlobalEntities[i];
            globalEntity.tick();
            if (globalEntity.dead)
            {
                GlobalEntities.RemoveAt(i--);
            }
        }

        Profiler.Stop("updateEntites.updateWeatherEffects");

        Profiler.Start("updateEntites.clearUnloadedEntities");
        for (int i = 0; i < _entitiesToUnload.Count; ++i)
        {
            Entity entityToUnload = _entitiesToUnload[i];
            int chunkX = entityToUnload.chunkX;
            int chunkZ = entityToUnload.chunkZ;

            if (entityToUnload.isPersistent && _host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
            {
                _host.GetChunk(chunkX, chunkZ).RemoveEntity(entityToUnload);
            }
        }

        for (int i = 0; i < _entitiesToUnload.Count; ++i)
        {
            NotifyEntityRemoved(_entitiesToUnload[i]);
        }

        _entitiesToUnload.Clear();
        Profiler.Stop("updateEntites.clearUnloadedEntities");

        Profiler.Start("updateEntites.updateLoadedEntities");
        for (int i = 0; i < Entities.Count; ++i)
        {
            Entity entity = Entities[i];

            if (entity.vehicle != null)
            {
                if (!entity.vehicle.dead && Equals(entity.vehicle.passenger, entity))
                {
                    continue;
                }

                entity.vehicle.passenger = null;
                entity.vehicle = null;
            }

            if (!entity.dead)
            {
                UpdateEntity(entity, true);
            }

            if (entity.dead)
            {
                int chunkX = entity.chunkX;
                int chunkZ = entity.chunkZ;

                if (entity.isPersistent && _host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
                {
                    _host.GetChunk(chunkX, chunkZ).RemoveEntity(entity);
                }

                Entities.RemoveAt(i--);
                if (_entitiesById.TryGetValue(entity.id, out Entity? current) && ReferenceEquals(current, entity))
                    _entitiesById.Remove(entity.id);
                NotifyEntityRemoved(entity);
            }
        }

        Profiler.Stop("updateEntites.updateLoadedEntities");

        _processingDeferred = true;
        Profiler.Start("updateEntites.updateLoadedTileEntities");

        for (int i = BlockEntities.Count - 1; i >= 0; i--)
        {
            BlockEntity blockEntity = BlockEntities[i];
            if (!blockEntity.isRemoved())
            {
                blockEntity.tick();
            }

            if (blockEntity.isRemoved())
            {
                BlockEntities.RemoveAt(i);
                Chunk chunk = _host.GetChunk(blockEntity.X >> 4, blockEntity.Z >> 4);
                chunk?.RemoveBlockEntityAt(blockEntity.X & 15, blockEntity.Y, blockEntity.Z & 15);
            }
        }

        _processingDeferred = false;

        if (_blockEntityUpdateQueue.Count > 0)
        {
            foreach (BlockEntity queuedBlockEntity in _blockEntityUpdateQueue)
            {
                if (!queuedBlockEntity.isRemoved())
                {
                    if (!BlockEntities.Contains(queuedBlockEntity))
                    {
                        BlockEntities.Add(queuedBlockEntity);
                    }

                    Chunk chunk = _host.GetChunk(queuedBlockEntity.X >> 4, queuedBlockEntity.Z >> 4);
                    chunk?.SetBlockEntity(queuedBlockEntity.X & 15, queuedBlockEntity.Y, queuedBlockEntity.Z & 15, queuedBlockEntity);
                    OnBlockUpdateRequired?.Invoke(queuedBlockEntity.X, queuedBlockEntity.Y, queuedBlockEntity.Z);
                }
            }

            _blockEntityUpdateQueue.Clear();
        }

        Profiler.Stop("updateEntites.updateLoadedTileEntities");
    }

    public void UpdateEntity(Entity entity, bool requireLoaded)
    {
        if (OnEntityUpdating != null && !OnEntityUpdating.Invoke(entity))
        {
            return;
        }

        if (entity.dead)
        {
            return;
        }

        int blockX = MathHelper.Floor(entity.x);
        int blockZ = MathHelper.Floor(entity.z);
        const byte loadRadius = 32;

        if (!requireLoaded || _host.IsRegionLoaded(blockX - loadRadius, 0, blockZ - loadRadius, blockX + loadRadius, 128, blockZ + loadRadius))
        {
            entity.lastTickX = entity.x;
            entity.lastTickY = entity.y;
            entity.lastTickZ = entity.z;
            entity.prevYaw = entity.yaw;
            entity.prevPitch = entity.pitch;

            if (requireLoaded && entity.isPersistent)
            {
                if (entity.vehicle != null)
                {
                    entity.tickRiding();
                }
                else
                {
                    entity.tick();
                }
            }

            if (double.IsNaN(entity.x) || double.IsInfinity(entity.x))
            {
                entity.x = entity.lastTickX;
            }

            if (double.IsNaN(entity.y) || double.IsInfinity(entity.y))
            {
                entity.y = entity.lastTickY;
            }

            if (double.IsNaN(entity.z) || double.IsInfinity(entity.z))
            {
                entity.z = entity.lastTickZ;
            }

            if (double.IsNaN(entity.pitch) || double.IsInfinity(entity.pitch))
            {
                entity.pitch = entity.prevPitch;
            }

            if (double.IsNaN(entity.yaw) || double.IsInfinity(entity.yaw))
            {
                entity.yaw = entity.prevYaw;
            }

            int newChunkX = MathHelper.Floor(entity.x / 16.0D);
            int newChunkY = MathHelper.Floor(entity.y / 16.0D);
            int newChunkZ = MathHelper.Floor(entity.z / 16.0D);

            if (!entity.isPersistent || entity.chunkX != newChunkX || entity.chunkSlice != newChunkY || entity.chunkZ != newChunkZ)
            {
                if (entity.isPersistent && _host.ChunkSource.IsChunkLoaded(entity.chunkX, entity.chunkZ))
                {
                    _host.GetChunk(entity.chunkX, entity.chunkZ).RemoveEntity(entity, entity.chunkSlice);
                }

                if (_host.ChunkSource.IsChunkLoaded(newChunkX, newChunkZ))
                {
                    entity.isPersistent = true;
                    _host.GetChunk(newChunkX, newChunkZ).AddEntity(entity);
                }
                else
                {
                    entity.isPersistent = false;
                }
            }

            if (requireLoaded && entity.isPersistent && entity.passenger != null)
            {
                if (!entity.passenger.dead && entity.passenger.vehicle == entity)
                {
                    UpdateEntity(entity.passenger, true);
                }
                else
                {
                    entity.passenger.vehicle = null;
                    entity.passenger = null;
                }
            }
        }
    }

    public List<Box> GetEntityCollisions(Entity entity, Box area) => GetEntityCollisions(entity, area, new List<Box>());

    internal List<Box> GetEntityCollisionsScratch(Entity entity, Box area)
    {
        _tempCollisionBoxes ??= new List<Box>();
        _tempCollisionBoxes.Clear();
        return GetEntityCollisions(entity, area, _tempCollisionBoxes);
    }

    private List<Box> GetEntityCollisions(Entity entity, Box area, List<Box> collidingBoundingBoxes)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        for (int x = minX; x < maxX; ++x)
        {
            for (int z = minZ; z < maxZ; ++z)
            {
                if (_host.IsPosLoaded(x, 64, z))
                {
                    for (int y = minY - 1; y < maxY; ++y)
                    {
                        Block block = Block.Blocks[_blocks.GetBlockId(x, y, z)];
                        if (block != null)
                        {
                            block.addIntersectingBoundingBox(_blocks, x, y, z, area, collidingBoundingBoxes);
                        }
                    }
                }
            }
        }

        const double expansion = 0.25D;
        _tempCollisionEntities ??= new List<Entity>();
        _tempCollisionEntities.Clear();

        GetEntities(entity, area.Expand(expansion, expansion, expansion), _tempCollisionEntities);

        int collisionCount = 0;
        int maxCollisions = _rules.GetInt(DefaultRules.MaxCollisions);

        for (int i = 0; i < _tempCollisionEntities.Count; ++i)
        {
            if (collisionCount >= maxCollisions)
            {
                break;
            }

            Box? entityBox = _tempCollisionEntities[i].getBoundingBox();
            if (entityBox != null && entityBox.Value.Intersects(area))
            {
                collidingBoundingBoxes.Add(entityBox.Value);
                collisionCount++;
            }

            entityBox = entity.getCollisionAgainstShape(_tempCollisionEntities[i]);
            if (entityBox != null && entityBox.Value.Intersects(area))
            {
                collidingBoundingBoxes.Add(entityBox.Value);
                collisionCount++;
            }
        }

        return collidingBoundingBoxes;
    }

    public List<Entity> GetEntities(Entity? excludeEntity, Box area) => GetEntities(excludeEntity, area, new List<Entity>());

    internal List<Entity> GetEntitiesScratch(Entity? excludeEntity, Box area)
    {
        _tempCollisionEntitiesResult ??= new List<Entity>();
        _tempCollisionEntitiesResult.Clear();
        return GetEntities(excludeEntity, area, _tempCollisionEntitiesResult);
    }

    private List<Entity> GetEntities(Entity? excludeEntity, Box area, List<Entity> results)
    {
        int minChunkX = MathHelper.Floor((area.MinX - 2.0D) / 16.0D);
        int maxChunkX = MathHelper.Floor((area.MaxX + 2.0D) / 16.0D);
        int minChunkZ = MathHelper.Floor((area.MinZ - 2.0D) / 16.0D);
        int maxChunkZ = MathHelper.Floor((area.MaxZ + 2.0D) / 16.0D);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; ++chunkX)
        {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; ++chunkZ)
            {
                if (_host.ChunkSource.IsChunkLoaded(chunkX, chunkZ))
                {
                    _host.GetChunk(chunkX, chunkZ).CollectOtherEntities(excludeEntity, area, results);
                }
            }
        }

        return results;
    }

    public List<T> CollectEntitiesOfType<T>(Box area) where T : Entity
    {
        List<T> results = new();

        int minChunkX = MathHelper.Floor((area.MinX - 2.0D) / 16.0D);
        int maxChunkX = MathHelper.Floor((area.MaxX + 2.0D) / 16.0D);
        int minChunkZ = MathHelper.Floor((area.MinZ - 2.0D) / 16.0D);
        int maxChunkZ = MathHelper.Floor((area.MaxZ + 2.0D) / 16.0D);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; ++chunkX)
        {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; ++chunkZ)
            {
                if (_host.HasChunk(chunkX, chunkZ))
                {
                    _host.GetChunk(chunkX, chunkZ).CollectEntitiesOfType(area, results);
                }
            }
        }

        return results;
    }

    public int CountEntitiesOfType(Type type)
    {
        int res = 0;
        foreach (Entity entity in Entities)
        {
            if (type.IsInstanceOfType(entity))
            {
                res++;
            }
        }

        return res;
    }

    public void AddEntities(List<Entity> entitiesToAdd)
    {
        Entities.AddRange(entitiesToAdd);
        for (int i = 0; i < entitiesToAdd.Count; ++i)
        {
            _entitiesById[entitiesToAdd[i].id] = entitiesToAdd[i];
            NotifyEntityAdded(entitiesToAdd[i]);
        }
    }

    public void UnloadEntities(List<Entity> entitiesToUnload) => _entitiesToUnload.AddRange(entitiesToUnload);

    public EntityPlayer GetClosestPlayer(double x, double y, double z, double range)
    {
        double minDistanceSquared = -1.0D;
        EntityPlayer closestPlayer = null;

        for (int i = 0; i < Players.Count; ++i)
        {
            EntityPlayer player = Players[i];
            double distanceSquared = player.getSquaredDistance(x, y, z);

            bool withinRange = range < 0.0D || distanceSquared < range * range;
            bool isClosestSoFar = minDistanceSquared == -1.0D || distanceSquared < minDistanceSquared;

            if (withinRange && isClosestSoFar)
            {
                minDistanceSquared = distanceSquared;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }

    public EntityPlayer? GetPlayer(string name) => Players.FirstOrDefault(p => p.name == name);

    public Entity? GetEntityByID(int id) => _entitiesById.TryGetValue(id, out Entity? entity) ? entity : null;

    public void UpdateSleepingPlayers()
    {
        AllPlayersSleeping = Players.Count > 0;
        foreach (EntityPlayer player in Players)
        {
            if (!player.isSleeping())
            {
                AllPlayersSleeping = false;
                break;
            }
        }
    }

    public bool CanSpawnEntity(Box spawnArea)
    {
        List<Entity> nearbyEntities = GetEntitiesScratch(null, spawnArea);
        return nearbyEntities.All(entity => entity.dead || !entity.preventEntitySpawning);
    }

    public void LoadChunksNearEntity(Entity entity)
    {
        int chunkX = MathHelper.Floor(entity.x / 16.0D);
        int chunkZ = MathHelper.Floor(entity.z / 16.0D);
        const byte loadRadius = 2;

        for (int x = chunkX - loadRadius; x <= chunkX + loadRadius; ++x)
        {
            for (int z = chunkZ - loadRadius; z <= chunkZ + loadRadius; ++z)
            {
                _host.GetChunk(x, z);
            }
        }

        if (!Entities.Contains(entity))
        {
            Entities.Add(entity);
        }
    }

    public BlockEntity? GetBlockEntity(int x, int y, int z)
    {
        Chunk chunk = _host.GetChunk(x >> 4, z >> 4);
        return chunk.GetBlockEntity(x & 15, y, z & 15) ?? BlockEntities.FirstOrDefault(e => e.X == x && e.Y == y && e.Z == z);
    }

    public void SetBlockEntity(int x, int y, int z, BlockEntity blockEntity)
    {
        if (!blockEntity.isRemoved())
        {
            if (_processingDeferred)
            {
                blockEntity.X = x;
                blockEntity.Y = y;
                blockEntity.Z = z;
                _blockEntityUpdateQueue.Add(blockEntity);
            }
            else
            {
                BlockEntities.Add(blockEntity);
                _host.GetChunk(x >> 4, z >> 4)?.SetBlockEntity(x & 15, y, z & 15, blockEntity);
            }
        }
    }

    public void RemoveBlockEntity(int x, int y, int z)
    {
        BlockEntity? entity = GetBlockEntity(x, y, z);
        if (entity != null && _processingDeferred)
        {
            entity.markRemoved();
        }
        else
        {
            _host.GetChunk(x >> 4, z >> 4)?.RemoveBlockEntityAt(x & 15, y, z & 15);
            if (entity != null)
            {
                BlockEntities.Remove(entity);
            }
        }
    }

    public void ProcessBlockUpdates(IEnumerable<BlockEntity> updates)
    {
        if (_processingDeferred)
        {
            _blockEntityUpdateQueue.AddRange(updates);
        }
        else
        {
            BlockEntities.AddRange(updates);
        }
    }
}
