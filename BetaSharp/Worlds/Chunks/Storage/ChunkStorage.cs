namespace BetaSharp.Worlds.Chunks.Storage;

public interface IChunkStorage
{
    Chunk LoadChunk(World world, int chunkX, int chunkZ, Chunk? reusedChunk = null);

    void SaveChunk(World world, Chunk chunk, Action onSave, long sequence);

    void SaveEntities(World world, Chunk chunk);

    void Tick();

    void Flush();

    void FlushToDisk();
}