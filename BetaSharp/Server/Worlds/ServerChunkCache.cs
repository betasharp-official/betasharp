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
    public bool forceLoad = false;
    private readonly Dictionary<int, Chunk> _chunksByPos = [];
    private readonly List<Chunk> _chunks = [];
    private readonly ServerWorld _world;
    private readonly Queue<Chunk> _chunkPool = new();

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

    public void isLoaded(int chunkX, int chunkZ)
    {
        Vec3i var3 = _world.getSpawnPos();
        int var4 = chunkX * 16 + 8 - var3.X;
        int var5 = chunkZ * 16 + 8 - var3.Z;
        short var6 = 128;
        if (var4 < -var6 || var4 > var6 || var5 < -var6 || var5 > var6)
        {
            _chunksToUnload.Add(ChunkPos.GetHashCode(chunkX, chunkZ));
        }
    }


    public Chunk LoadChunk(int chunkX, int chunkZ, Chunk? reusedChunk = null)
    {
        int var3 = ChunkPos.GetHashCode(chunkX, chunkZ);
        _chunksToUnload.Remove(var3);
        _chunksByPos.TryGetValue(var3, out Chunk? var4);
        if (var4 == null)
        {
            Chunk? pooledChunk = reusedChunk;
            if (pooledChunk == null)
            {
                _chunkPool.TryDequeue(out pooledChunk);
            }

            var4 = LoadChunkFromStorage(chunkX, chunkZ, pooledChunk);
            if (var4 == null)
            {
                if (_generator == null)
                {
                    var4 = _empty;
                }
                else
                {
                    var4 = _generator.GetChunk(chunkX, chunkZ, pooledChunk);
                }
            }

            _chunksByPos.Add(var3, var4);
            _chunks.Add(var4);
            if (var4 != null)
            {
                var4.PopulateBlockLight();
                var4.Load();
            }

            if (!var4.TerrainPopulated
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

        return var4;
    }


    public Chunk GetChunk(int chunkX, int chunkZ, Chunk? reusedChunk = null)
    {
        _chunksByPos.TryGetValue(ChunkPos.GetHashCode(chunkX, chunkZ), out Chunk? var3);
        if (var3 == null)
        {
            return !_world.eventProcessingEnabled && !forceLoad ? _empty : LoadChunk(chunkX, chunkZ, reusedChunk);
        }
        else
        {
            return var3;
        }
    }

    private Chunk? LoadChunkFromStorage(int chunkX, int chunkZ, Chunk? reusedChunk = null)
    {
        if (_storage == null)
        {
            return null;
        }
        else
        {
            try
            {
                Chunk chunk = _storage.LoadChunk(_world, chunkX, chunkZ, reusedChunk);
                chunk?.LastSaveTime = _world.getTime();

                return chunk;
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
            catch (java.io.IOException ex)
            {
                ex.printStackTrace();
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
        int var3 = 0;

        for (int var4 = 0; var4 < _chunks.Count; var4++)
        {
            Chunk var5 = _chunks[var4];
            if (saveEntities && !var5.Empty)
            {
                this.saveEntities(var5);
            }

            if (var5.ShouldSave(saveEntities))
            {
                saveChunk(var5);
                var5.Dirty = false;
                if (++var3 == 24 && !saveEntities)
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
            for (int var1 = 0; var1 < 100; var1++)
            {
                if (_chunksToUnload.Count > 0)
                {
                    int chunkPos = _chunksToUnload.First();
                    Chunk chunk = _chunksByPos[chunkPos];
                    chunk.Unload();
                    saveChunk(chunk);
                    saveEntities(chunk);
                    _chunksToUnload.Remove(chunkPos);
                    _chunksByPos.Remove(chunkPos);
                    _chunks.Remove(chunk);

                    if (chunk != _empty)
                    {
                        chunk.Reset(chunk.X, chunk.Z);
                        _chunkPool.Enqueue(chunk);
                    }
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
        return $"ServerChunkCache: {_chunksByPos.Count} ({_chunkPool.Count} pooled)";
    }

    public void ClearCache()
    {
        _chunkPool.Clear();
    }

    public ChunkSource CreateParallelGenerator() => _generator.CreateParallelInstance();

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
