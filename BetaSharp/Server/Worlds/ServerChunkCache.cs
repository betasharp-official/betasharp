using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Chunks.Storage;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Worlds;

public class ServerChunkCache : ChunkSource
{
    private readonly ILogger<ServerChunkCache> _logger = Log.Instance.For<ServerChunkCache>();
    private readonly HashSet<int> _chunksToUnload = [];
    private readonly Chunk _empty;
    private readonly ChunkSource _generator;
    private readonly IChunkStorage _storage;
    public bool ForceLoad = false;
    private readonly Dictionary<int, Chunk> _chunksByPos = [];
    private readonly List<Chunk> _chunks = [];
    private readonly ServerWorld _world;

    public ServerChunkCache(ServerWorld world, IChunkStorage storage, ChunkSource generator)
    {
        _empty = new EmptyChunk(world, new byte[32768], 0, 0);
        _world = world;
        _storage = storage;
        _generator = generator;
    }


    public bool IsChunkLoaded(int x, int z)
    {
        return _chunksByPos.ContainsKey(ChunkPos.GetHashCode(x, z));
    }

    public void QueueUnloadCheck(int chunkX, int chunkZ)
    {
        Vec3i spawnPos = _world.getSpawnPos();
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
        int chunkKey = ChunkPos.GetHashCode(chunkX, chunkZ);
        _chunksToUnload.Remove(chunkKey);
        _chunksByPos.TryGetValue(chunkKey, out Chunk? chunk);
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

            _chunksByPos.Add(chunkKey, chunk);
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
            return !_world.eventProcessingEnabled && !ForceLoad ? _empty : LoadChunk(chunkX, chunkZ);
        }
        else
        {
            return chunk;
        }
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
                loadedChunk?.LastSaveTime = _world.getTime();

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
                chunk.LastSaveTime = _world.getTime();
                _storage.SaveChunk(_world, chunk, null, -1);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Exception");
            }
        }
    }


    public void DecorateTerrain(ChunkSource source, int x, int z)
    {
        Chunk var4 = GetChunk(x, z);
        if (!var4.TerrainPopulated)
        {
            var4.TerrainPopulated = true;
            if (_generator != null)
            {
                _generator.DecorateTerrain(source, x, z);
                var4.MarkDirty();
            }
        }
    }

    public bool Save(bool saveEntities, LoadingDisplay display)
    {
        int savedCount = 0;

        for (int i = 0; i < _chunks.Count; i++)
        {
            Chunk chunk = _chunks[i];
            if (saveEntities && !chunk.Empty)
            {
                this.saveEntities(chunk);
            }

            if (chunk.ShouldSave(saveEntities))
            {
                saveChunk(chunk);
                chunk.Dirty = false;
                if (++savedCount == 24 && !saveEntities)
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
            for (int i = 0; i < 100; i++)
            {
                if (_chunksToUnload.Count > 0)
                {
                    int chunkKey = _chunksToUnload.First();
                    Chunk chunk = _chunksByPos[chunkKey];
                    chunk.Unload();
                    saveChunk(chunk);
                    saveEntities(chunk);
                    _chunksToUnload.Remove(chunkKey);
                    _chunksByPos.Remove(chunkKey);
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

    public void markChunksForUnload(int renderDistanceChunks)
    {
    }

    public string GetDebugInfo()
    {
        return "NOP";
    }
}
