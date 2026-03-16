using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Storage.RegionFormat;

public interface IChunkStorage
{
    Chunk? LoadChunk(IWorldContext world, int chunkX, int chunkZ);

    void SaveChunk(IWorldContext world, Chunk chunk, Action onSave, long sequence);

    void SaveEntities(IWorldContext world, Chunk chunk);

    void Tick();

    void Flush();

    void FlushToDisk();
}
