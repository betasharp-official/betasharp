using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds.Core.Systems;

public sealed class ChunkHost(IChunkSource chunkSource)
{
    public IChunkSource ChunkSource => chunkSource;

    public bool HasChunk(int x, int z) => chunkSource.IsChunkLoaded(x, z);

    public Chunk GetChunkFromPos(int x, int z) => GetChunk(x >> 4, z >> 4);

    public Chunk GetChunk(int chunkX, int chunkZ) => chunkSource.GetChunk(chunkX, chunkZ);

    public bool IsPosLoaded(int x, int y, int z) => y is >= 0 and < 128 && HasChunk(x >> 4, z >> 4);

    public bool IsRegionLoaded(int x, int y, int z, int range) => IsRegionLoaded(x - range, y - range, z - range, x + range, y + range, z + range);

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
                    if (!HasChunk(x, z))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    public byte[] GetChunkData(int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        byte[] chunkData = new byte[sizeX * sizeY * sizeZ * 5 / 2];

        int startChunkX = x >> 4;
        int startChunkZ = z >> 4;
        int endChunkX = (x + sizeX - 1) >> 4;
        int endChunkZ = (z + sizeZ - 1) >> 4;

        int currentBufferOffset = 0;
        int minY = Math.Max(0, y);
        int maxY = Math.Min(128, y + sizeY);

        for (int chunkX = startChunkX; chunkX <= endChunkX; chunkX++)
        {
            int localStartX = Math.Max(0, x - chunkX * 16);
            int localEndX = Math.Min(16, x + sizeX - chunkX * 16);

            for (int chunkZ = startChunkZ; chunkZ <= endChunkZ; chunkZ++)
            {
                int localStartZ = Math.Max(0, z - chunkZ * 16);
                int localEndZ = Math.Min(16, z + sizeZ - chunkZ * 16);

                currentBufferOffset = GetChunk(chunkX, chunkZ).ToPacket(
                    chunkData,
                    localStartX, minY, localStartZ,
                    localEndX, maxY, localEndZ,
                    currentBufferOffset);
            }
        }

        return chunkData;
    }
}
