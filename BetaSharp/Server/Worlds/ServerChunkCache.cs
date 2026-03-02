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


    public Chunk LoadChunk(int chunkX, int chunkZ)
    {
        int var3 = ChunkPos.GetHashCode(chunkX, chunkZ);
        _chunksToUnload.Remove(var3);
        _chunksByPos.TryGetValue(var3, out Chunk? var4);
        if (var4 == null)
        {
            var4 = LoadChunkFromStorage(chunkX, chunkZ);
            if (var4 == null)
            {
                if (_generator == null)
                {
                    var4 = _empty;
                }
                else
                {
                    var4 = _generator.GetChunk(chunkX, chunkZ);
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


    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        _chunksByPos.TryGetValue(ChunkPos.GetHashCode(chunkX, chunkZ), out Chunk? var3);
        if (var3 == null)
        {
            return !_world.eventProcessingEnabled && !forceLoad ? _empty : LoadChunk(chunkX, chunkZ);
        }
        else
        {
            return var3;
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
                Chunk var3 = _storage.LoadChunk(_world, chunkX, chunkZ);
                var3?.LastSaveTime = _world.getTime();

                return var3;
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
                    int var2 = _chunksToUnload.First();
                    Chunk var3 = _chunksByPos[var2];
                    var3.Unload();
                    saveChunk(var3);
                    saveEntities(var3);
                    _chunksToUnload.Remove(var2);
                    _chunksByPos.Remove(var2);
                    _chunks.Remove(var3);
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

    // Exposes storage loading so the parallel gen phase can skip already-saved chunks.
    public Chunk? TryLoadFromStorage(int chunkX, int chunkZ) => LoadChunkFromStorage(chunkX, chunkZ);

    /// Creates a parallel-safe generator instance for off-thread terrain generation.
    public ChunkSource CreateParallelGenerator() => _generator.CreateParallelInstance();

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
