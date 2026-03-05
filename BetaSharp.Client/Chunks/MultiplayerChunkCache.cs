using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Client.Chunks;

public class MultiplayerChunkCache(World world) : ChunkSource
{
    private readonly Chunk _empty = new EmptyChunk(world, new byte[-short.MinValue], 0, 0);
    private readonly Dictionary<ChunkPos, Chunk> _chunkByPos = [];

    public bool IsChunkLoaded(int x, int y) => _chunkByPos.ContainsKey(new ChunkPos(x, y));

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

        chunk.Loaded = true;
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

    public void DecorateTerrain(ChunkSource source, int x, int y){ }

    public void markChunksForUnload(int _) { }

    public string GetDebugInfo() => $"MultiplayerChunkCache: {_chunkByPos.Count}";
}
