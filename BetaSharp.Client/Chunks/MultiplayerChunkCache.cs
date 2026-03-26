using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Chunks;

public class MultiplayerChunkCache(World world) : IChunkSource
{
    private readonly Chunk _empty = new EmptyChunk(world, new byte[-short.MinValue], 0, 0);
    private readonly Dictionary<ChunkPos, Chunk> _chunkByPos = [];

    public bool IsChunkLoaded(int x, int z)
    {
        return _chunkByPos.ContainsKey(new ChunkPos(x, z));
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
        byte[] blocks = new byte[-short.MinValue];
        Chunk chunk = new(world, blocks, x, z);

        Array.Fill(chunk.SkyLight.Bytes, (byte)255);
        _chunkByPos[key] = chunk;

        // Keep it "unloaded" until the server sends ChunkData / deltas.
        // This prevents the client from running local physics/collision against
        // an incomplete terrain state.
        chunk.Loaded = false;
        return chunk;
    }

    public Chunk GetChunk(int x, int z)
    {
        _chunkByPos.TryGetValue(new ChunkPos(x, z), out Chunk? chunk);
        return chunk ?? _empty;
    }

    public bool Save(bool bl, LoadingDisplay display) => true;

    public bool Tick() => false;

    public bool CanSave() => false;

    public void DecorateTerrain(IChunkSource source, int x, int y){ }

    public void markChunksForUnload(int _) { }

    public string GetDebugInfo() => $"MultiplayerChunkCache: {_chunkByPos.Count}";
}
