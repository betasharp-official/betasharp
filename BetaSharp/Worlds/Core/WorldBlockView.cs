using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Generation.Biomes.Source;

namespace BetaSharp.Worlds.Core;

/// <summary>
/// Read-focused view of the world's block data. Implements <see cref="IBlockReader"/>
/// by delegating to <see cref="BlockHost"/>, and exposes write methods that forward
/// to the associated <see cref="WorldBlockWrite"/> so that block code can use a single
/// world parameter for both reads and writes.
/// </summary>
public class WorldBlockView : IBlockReader
{
    private readonly BlockHost _host;
    private readonly Dimension _dimension;
    private readonly bool _isRemote;
    public int AmbientDarkness;



    public WorldBlockView(BlockHost host, Dimension dimension, WorldBlockWrite writer, bool isRemote)
    {
        _host = host;
        _isRemote = isRemote;
        _dimension = dimension;
    }

    // --- IBlockReader (delegated to host) ---

    public int GetBlockId(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128) return 0;
        return _host.GetChunk(x >> 4, z >> 4).GetBlockId(x & 15, y, z & 15);
    }

    public int GetBlockMeta(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128) return 0;
        return _host.GetChunk(x >> 4, z >> 4).GetBlockMeta(x & 15, y, z & 15);
    }

    public Material GetMaterial(int x, int y, int z)
    {
        int blockId = GetBlockId(x, y, z);
        return blockId == 0 ? Material.Air : Block.Blocks[blockId].material;
    }

    public BlockEntity? GetBlockEntity(int x, int y, int z)
    {
        Chunk? chunk = _host.GetChunk(x >> 4, z >> 4);
        return chunk?.GetBlockEntity(x & 15, y, z & 15);
    }

    public bool IsOpaque(int x, int y, int z)
    {
        Block? block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.isOpaque();
    }

    public bool ShouldSuffocate(int x, int y, int z)
    {
        Block? block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.material.Suffocates && block.isFullCube();
    }

    public BiomeSource GetBiomeSource() => _dimension.BiomeSource;

    public bool IsAir(int x, int y, int z) => GetBlockId(x, y, z) == 0;

    public int GetBrightness(int x, int y, int z)
    {
        if (y < 0) return 0;
        if (y >= 128) return !_dimension.HasCeiling ? 15 : 0;
        return _host.GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, 0);
    }

    public bool IsTopY(int x, int y, int z)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y < 0) return false;
            if (y >= 128) return true;
            if (!_host.HasChunk(x >> 4, z >> 4)) return false;

            Chunk chunk = _host.GetChunk(x >> 4, z >> 4);
            return chunk.IsAboveMaxHeight(x & 15, y, z & 15);
        }

        return false;
    }

    public int GetTopY(int x, int z)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            int chunkX = x >> 4;
            int chunkZ = z >> 4;

            if (!_host.HasChunk(chunkX, chunkZ)) return 0;

            Chunk chunk = _host.GetChunk(chunkX, chunkZ);
            return chunk.GetHeight(x & 15, z & 15);
        }

        return 0;
    }

    public int GetTopSolidBlockY(int x, int z)
    {
        Chunk chunk = _host.GetChunkFromPos(x, z);
        int currentY = 127;
        int localX = x & 15;
        int localZ = z & 15;

        for (; currentY > 0; --currentY)
        {
            int blockId = chunk.GetBlockId(localX, currentY, localZ);
            Material material = blockId == 0 ? Material.Air : Block.Blocks[blockId].material;

            if (material.BlocksMovement || material.IsFluid)
            {
                return currentY + 1;
            }
        }

        return -1;
    }

    public int GetSpawnPositionValidityY(int x, int z)
    {
        Chunk chunk = _host.GetChunkFromPos(x, z);
        int currentY = 127;
        int localX = x & 15;
        int localZ = z & 15;

        for (; currentY > 0; currentY--)
        {
            int blockId = chunk.GetBlockId(localX, currentY, localZ);
            if (blockId != 0 && Block.Blocks[blockId].material.BlocksMovement)
            {
                return currentY + 1;
            }
        }

        return -1;
    }
}
