using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Storage.RegionFormat;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Worlds;

public class ServerChunkCache : IChunkSource
{
    private readonly ILogger<ServerChunkCache> _logger = Log.Instance.For<ServerChunkCache>();
    private readonly HashSet<int> _chunksToUnload = [];
    private readonly Chunk _empty;
    private readonly IChunkSource _generator;
    private readonly IChunkStorage _storage;
    public bool forceLoad = false;
    private readonly Dictionary<int, Chunk> _chunksByPos = [];
    private readonly List<Chunk> _chunks = [];
    private readonly ServerWorld _world;

    public ServerChunkCache(ServerWorld world, IChunkStorage storage, IChunkSource generator)
    {
        _empty = new EmptyChunk(world, new byte[ChuckFormat.ChunkSize], 0, 0);
        _world = world;
        _storage = storage;
        _generator = generator;
    }


    public bool IsChunkLoaded(int x, int z)
    {
        return _chunksByPos.ContainsKey(ChunkPos.GetHashCode(x, z));
    }

    public void isLoaded(int chunkX, int chunkZ)
    {
        Vec3i spawnPos = _world.Properties.GetSpawnPos();
        int deltaX = chunkX * 16 + 8 - spawnPos.X;
        int deltaZ = chunkZ * 16 + 8 - spawnPos.Z;
        short spawnRadius = 128;
        if (deltaX < -spawnRadius || deltaX > spawnRadius || deltaZ < -spawnRadius || deltaZ > spawnRadius)
        {
            _chunksToUnload.Add(ChunkPos.GetHashCode(chunkX, chunkZ));
        }
    }


    public Chunk LoadChunk(int chunkX, int chunkZ)
    {
        int hash = ChunkPos.GetHashCode(chunkX, chunkZ);
        _chunksToUnload.Remove(hash);
        _chunksByPos.TryGetValue(hash, out Chunk? chunk);
        if (chunk == null)
        {
            chunk = LoadChunkFromStorage(chunkX, chunkZ);
            if (chunk == null)
            {
                if (_generator == null)
                {
                    chunk = _empty;
                }
                else
                {
                    chunk = _generator.GetChunk(chunkX, chunkZ);
                }
            }

            _chunksByPos.Add(hash, chunk);
            _chunks.Add(chunk);
            if (chunk != null)
            {
                chunk.PopulateBlockLight();
                chunk.Load();
            }

            if (!chunk.TerrainPopulated
                && IsChunkLoaded(chunkX + 1, chunkZ + 1)
                && IsChunkLoaded(chunkX, chunkZ + 1)
                && IsChunkLoaded(chunkX + 1, chunkZ))
            {
                DecorateTerrain(this, chunkX, chunkZ);
            }

            if (IsChunkLoaded(chunkX - 1, chunkZ)
                && !GetChunk(chunkX - 1, chunkZ).TerrainPopulated
                && IsChunkLoaded(chunkX - 1, chunkZ + 1)
                && IsChunkLoaded(chunkX, chunkZ + 1)
                && IsChunkLoaded(chunkX - 1, chunkZ))
            {
                DecorateTerrain(this, chunkX - 1, chunkZ);
            }

            if (IsChunkLoaded(chunkX, chunkZ - 1)
                && !GetChunk(chunkX, chunkZ - 1).TerrainPopulated
                && IsChunkLoaded(chunkX + 1, chunkZ - 1)
                && IsChunkLoaded(chunkX, chunkZ - 1)
                && IsChunkLoaded(chunkX + 1, chunkZ))
            {
                DecorateTerrain(this, chunkX, chunkZ - 1);
            }

            if (IsChunkLoaded(chunkX - 1, chunkZ - 1)
                && !GetChunk(chunkX - 1, chunkZ - 1).TerrainPopulated
                && IsChunkLoaded(chunkX - 1, chunkZ - 1)
                && IsChunkLoaded(chunkX, chunkZ - 1)
                && IsChunkLoaded(chunkX - 1, chunkZ))
            {
                DecorateTerrain(this, chunkX - 1, chunkZ - 1);
            }
        }

        return chunk;
    }


    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        _chunksByPos.TryGetValue(ChunkPos.GetHashCode(chunkX, chunkZ), out Chunk? chunk);
        if (chunk == null)
        {
            return !_world.EventProcessingEnabled && !forceLoad ? _empty : LoadChunk(chunkX, chunkZ);
        }

        return chunk;
    }

    private Chunk? LoadChunkFromStorage(int chunkX, int chunkZ)
    {
        if (_storage == null)
        {
            return null;
        }
        else
        {
            try
            {
                Chunk loadedChunk = _storage.LoadChunk(_world, chunkX, chunkZ);
                loadedChunk?.LastSaveTime = _world.GetTime();

                return loadedChunk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception");
                return null;
            }
        }
    }

    private void saveEntities(Chunk chunk)
    {
        if (_storage != null)
        {
            try
            {
                _storage.SaveEntities(_world, chunk);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception");
            }
        }
    }

    private void saveChunk(Chunk chunk)
    {
        if (_storage != null)
        {
            try
            {
                chunk.LastSaveTime = _world.GetTime();
                _storage.SaveChunk(_world, chunk, null, -1);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Exception");
            }
        }
    }


    public void DecorateTerrain(IChunkSource source, int x, int z)
    {
        Chunk chunk = GetChunk(x, z);
        if (!chunk.TerrainPopulated)
        {
            chunk.TerrainPopulated = true;
            if (_generator != null)
            {
                _generator.DecorateTerrain(source, x, z);
                chunk.MarkDirty();
                _world.ChunkMap.OnChunkDecorated(x, z);
            }
        }
    }

    public bool Save(bool saveEntities, LoadingDisplay display)
    {
        int savedChunkCount = 0;

        for (int chunkIndex = 0; chunkIndex < _chunks.Count; chunkIndex++)
        {
            Chunk chunk = _chunks[chunkIndex];
            if (saveEntities && !chunk.Empty)
            {
                this.saveEntities(chunk);
            }

            if (chunk.ShouldSave(saveEntities))
            {
                saveChunk(chunk);
                chunk.Dirty = false;
                if (++savedChunkCount == 24 && !saveEntities)
                {
                    return false;
                }
            }
        }

        if (saveEntities)
        {
            if (_storage == null)
            {
                return true;
            }

            _storage.Flush();
        }

        return true;
    }


    public bool Tick()
    {
        if (!_world.savingDisabled)
        {
            for (int unloadIndex = 0; unloadIndex < 100; unloadIndex++)
            {
                if (_chunksToUnload.Count > 0)
                {
                    int chunkHash = _chunksToUnload.First();
                    Chunk chunk = _chunksByPos[chunkHash];
                    chunk.Unload();
                    saveChunk(chunk);
                    saveEntities(chunk);
                    _chunksToUnload.Remove(chunkHash);
                    _chunksByPos.Remove(chunkHash);
                    _chunks.Remove(chunk);
                }
            }

            _storage?.Tick();
        }

        return _generator.Tick();
    }


    public bool CanSave()
    {
        return !_world.savingDisabled;
    }

    public string GetDebugInfo()
    {
        return "NOP";
    }

    /// Creates a parallel-safe generator instance for off-thread terrain generation.
    public IChunkSource CreateParallelGenerator() => _generator.CreateParallelInstance();

    // Inserts a pre-generated chunk without triggering terrain re-generation.
    // Checks storage first so that saved data is used correctly on server restart.
    public void InsertPreGeneratedChunk(int chunkX, int chunkZ, Chunk generatedChunk)
    {
        int key = ChunkPos.GetHashCode(chunkX, chunkZ);
        _chunksToUnload.Remove(key);
        if (_chunksByPos.ContainsKey(key)) return;
        Chunk chunk = LoadChunkFromStorage(chunkX, chunkZ) ?? generatedChunk;
        _chunksByPos.Add(key, chunk);
        _chunks.Add(chunk);
        chunk.PopulateBlockLight();
        chunk.Load();
    }

    // Runs the 4 decoration neighbour checks for a newly inserted chunk,
    // mirroring the logic in LoadChunk but without re-generating terrain.

    public void DecorateIfReady(int chunkX, int chunkZ)
    {
        if (!IsChunkLoaded(chunkX, chunkZ)) return;

        if (!GetChunk(chunkX, chunkZ).TerrainPopulated
            && IsChunkLoaded(chunkX + 1, chunkZ + 1)
            && IsChunkLoaded(chunkX, chunkZ + 1)
            && IsChunkLoaded(chunkX + 1, chunkZ))
            DecorateTerrain(this, chunkX, chunkZ);

        if (IsChunkLoaded(chunkX - 1, chunkZ)
            && !GetChunk(chunkX - 1, chunkZ).TerrainPopulated
            && IsChunkLoaded(chunkX - 1, chunkZ + 1)
            && IsChunkLoaded(chunkX, chunkZ + 1)
            && IsChunkLoaded(chunkX - 1, chunkZ))
            DecorateTerrain(this, chunkX - 1, chunkZ);

        if (IsChunkLoaded(chunkX, chunkZ - 1)
            && !GetChunk(chunkX, chunkZ - 1).TerrainPopulated
            && IsChunkLoaded(chunkX + 1, chunkZ - 1)
            && IsChunkLoaded(chunkX, chunkZ - 1)
            && IsChunkLoaded(chunkX + 1, chunkZ))
            DecorateTerrain(this, chunkX, chunkZ - 1);

        if (IsChunkLoaded(chunkX - 1, chunkZ - 1)
            && !GetChunk(chunkX - 1, chunkZ - 1).TerrainPopulated
            && IsChunkLoaded(chunkX - 1, chunkZ - 1)
            && IsChunkLoaded(chunkX, chunkZ - 1)
            && IsChunkLoaded(chunkX - 1, chunkZ))
            DecorateTerrain(this, chunkX - 1, chunkZ - 1);
    }
}
