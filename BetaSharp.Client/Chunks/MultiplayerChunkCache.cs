using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Client.Chunks;

public class MultiplayerChunkCache : ChunkSource
{

    private readonly Chunk empty;
    private readonly Dictionary<ChunkPos, Chunk> chunkByPos = [];
    private readonly World world;

    public MultiplayerChunkCache(World world)
    {
        empty = new EmptyChunk(world, new byte[-short.MinValue], 0, 0);
        this.world = world;
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
            return chunkByPos.ContainsKey(key);
        }
    }

    public void UnloadChunk(int x, int z)
    {
        Chunk chunk = GetChunk(x, z);
        if (!chunk.IsEmpty())
        {
            chunk.Unload();
        }

        chunkByPos.Remove(new ChunkPos(x, z));
    }

    public Chunk LoadChunk(int x, int z)
    {
        ChunkPos key = new(x, z);
        byte[] blocks = new byte[-short.MinValue];
        Chunk chunk = new(world, blocks, x, z);
        Array.Fill<byte>(chunk.SkyLight.Bytes, 255);

        if (chunkByPos.ContainsKey(key))
        {
            chunkByPos[key] = chunk;
        }
        else
        {
            chunkByPos.Add(key, chunk);
        }

        chunk.Loaded = true;
        return chunk;
    }

    public Chunk GetChunk(int x, int z)
    {
        ChunkPos key = new(x, z);
        chunkByPos.TryGetValue(key, out Chunk? chunk);
        return chunk == null ? empty : chunk;
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

    public void markChunksForUnload(int _)
    {
    }

    public string GetDebugInfo()
    {
        return "MultiplayerChunkCache: " + chunkByPos.Count;
    }
}