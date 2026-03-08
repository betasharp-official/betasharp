using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds.Core;

public sealed class BlockHost
{
    private readonly IChunkSource _chunkSource;


    public BlockHost(IChunkSource chunkSource)
    {
        _chunkSource = chunkSource;
    }

    public IChunkSource ChunkSource => _chunkSource;

    // --- Chunk Access ---

    public bool HasChunk(int x, int z) => _chunkSource.IsChunkLoaded(x, z);

    public Chunk GetChunkFromPos(int x, int z) => GetChunk(x >> 4, z >> 4);

    public Chunk GetChunk(int chunkX, int chunkZ) => _chunkSource.GetChunk(chunkX, chunkZ);

    public bool IsPosLoaded(int x, int y, int z) => y is >= 0 and < 128 && HasChunk(x >> 4, z >> 4);

    public bool IsRegionLoaded(int x, int y, int z, int range) =>
        IsRegionLoaded(x - range, y - range, z - range, x + range, y + range, z + range);

    public bool IsRegionLoaded(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        if (maxY >= 0 && minY < 128)
        {
            minX >>= 4;
            minZ >>= 4;
            maxX >>= 4;
            maxZ >>= 4;

            for (int x = minX; x <= maxX; ++x)
            {
                for (int z = minZ; z <= maxZ; ++z)
                {
                    if (!HasChunk(x, z)) return false;
                }
            }

            return true;
        }

        return false;
    }
}
