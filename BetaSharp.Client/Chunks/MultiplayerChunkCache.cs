using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Client.Chunks;

public class MultiplayerChunkCache : ChunkSource
{

    private readonly Chunk _empty;
    private readonly Dictionary<ChunkPos, Chunk> _chunkByPos = [];
    private readonly World _world;

    public MultiplayerChunkCache(World world)
    {
        _empty = new EmptyChunk(world, new byte[32768], 0, 0);
        _world = world;
    }

    public bool IsChunkLoaded(int x, int y)
    {
        if (this != null)
        {
            return true;
        }
        else
        {
            ChunkPos key = new(x, y);
            return _chunkByPos.ContainsKey(key);
        }
    }

    public void UnloadChunk(int x, int z)
    {
        Chunk chunk = GetChunk(x, z);
        if (!chunk.IsEmpty())
        {
            chunk.Unload();
        }

        _chunkByPos.Remove(new ChunkPos(x, z));
    }

    public Chunk LoadChunk(int x, int z)
    {
        ChunkPos key = new(x, z);
        byte[] blocks = new byte[32768];
        Chunk chunk = new(_world, blocks, x, z);
        Array.Fill<byte>(chunk.SkyLight.Bytes, 255);

        if (!_chunkByPos.TryAdd(key, chunk))
        {
            _chunkByPos[key] = chunk;
        }

        chunk.Loaded = true;
        return chunk;
    }

    public Chunk GetChunk(int x, int z)
    {
        ChunkPos key = new(x, z);
        _chunkByPos.TryGetValue(key, out Chunk? chunk);
        return chunk ?? _empty;
    }

    public bool Save(bool bl, LoadingDisplay display)
    {
        return true;
    }

    public bool Tick()
    {
        return false;
    }

    public bool CanSave()
    {
        return false;
    }

    public void DecorateTerrain(ChunkSource source, int x, int y)
    {
    }

    public string GetDebugInfo()
    {
        return "MultiplayerChunkCache: " + _chunkByPos.Count;
    }
}
