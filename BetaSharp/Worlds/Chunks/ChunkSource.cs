namespace BetaSharp.Worlds.Chunks;

public interface ChunkSource
{
    bool IsChunkLoaded(int x, int z);

    Chunk GetChunk(int x, int z);

    Chunk LoadChunk(int x, int z);

    void DecorateTerrain(ChunkSource source, int x, int z);

    bool Save(bool saveEntities, LoadingDisplay display);

    bool Tick();

    bool CanSave();

    string GetDebugInfo();

    // Creates a new generator instance that is safe to use on a separate thread.
    // The default returns the same instance (not parallel-safe for stateful generators).
    ChunkSource CreateParallelInstance() => this;
}
