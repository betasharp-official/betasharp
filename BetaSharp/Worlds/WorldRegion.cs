using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds;

internal class WorldRegion : IBlockAccess
{
    private readonly int _chunkX;
    private readonly int _chunkZ;
    private readonly Chunk[][] _chunks;
    private readonly World _world;

    public WorldRegion(World world, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        _world = world;
        _chunkX = minX >> 4;
        _chunkZ = minZ >> 4;
        int endX = maxX >> 4;
        int endZ = maxZ >> 4;

        int width = endX - _chunkX + 1;
        int depth = endZ - _chunkZ + 1;

        _chunks = new Chunk[width][];
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i] = new Chunk[depth];
        }

        for (int cx = _chunkX; cx <= endX; ++cx)
        {
            for (int cz = _chunkZ; cz <= endZ; ++cz)
            {
                _chunks[cx - _chunkX][cz - _chunkZ] = world.GetChunk(cx, cz);
            }
        }

    }

    public int getBlockId(int x, int y, int z)
    {
        if (y is < 0 or >= 128) return 0;

        int cx = (x >> 4) - _chunkX;
        int cz = (z >> 4) - _chunkZ;

        if (cx >= 0 && cx < _chunks.Length && cz >= 0 && cz < _chunks[cx].Length)
        {
            Chunk chunk = _chunks[cx][cz];
            return chunk?.GetBlockId(x & 15, y, z & 15) ?? 0;
        }

        return 0;
    }

    public BlockEntity? getBlockEntity(int x, int y, int z)
    {
        int cx = (x >> 4) - _chunkX;
        int cz = (z >> 4) - _chunkZ;

        if (cx < 0 || cx >= _chunks.Length || cz < 0 || cz < 0 || cz >= _chunks[cx].Length)
            return null;

        return _chunks[cx][cz]?.GetBlockEntity(x & 15, y, z & 15);
    }

    public float getNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int finalLight = Math.Max(getRawBrightness(x, y, z), blockLight);
        return _world.dimension.LightLevelToLuminance[finalLight];
    }

    public float getLuminance(int x, int y, int z)
    {
        return _world.dimension.LightLevelToLuminance[getRawBrightness(x, y, z)];
    }

    public int getRawBrightness(int x, int y, int z)
    {
        return getRawBrightness(x, y, z, true);
    }

    public int getRawBrightness(int x, int y, int z, bool useNeighborLight)
    {
        // World bounds check
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return 15;
        if (useNeighborLight)
        {
            int id = getBlockId(x, y, z);
            if (id == Block.Slab.id || id == Block.Farmland.id || id == Block.WoodenStairs.id || id == Block.CobblestoneStairs.id)
            {
                int max = getRawBrightness(x, y + 1, z, false);
                max = Math.Max(max, getRawBrightness(x + 1, y, z, false));
                max = Math.Max(max, getRawBrightness(x - 1, y, z, false));
                max = Math.Max(max, getRawBrightness(x, y, z + 1, false));
                max = Math.Max(max, getRawBrightness(x, y, z - 1, false));
                return max;
            }
        }

        if (y < 0) return 0;
        if (y >= 128) return Math.Max(0, 15 - _world.ambientDarkness);

        int cIdxX = (x >> 4) - _chunkX;
        int cIdxZ = (z >> 4) - _chunkZ;

        return _chunks[cIdxX][cIdxZ].GetLight(x & 15, y, z & 15, _world.ambientDarkness);
    }

    public int getBlockMeta(int x, int y, int z)
    {
        if (y is < 0 or >= 128) return 0;

        int cx = (x >> 4) - _chunkX;
        int cz = (z >> 4) - _chunkZ;
        return _chunks[cx][cz].GetBlockMeta(x & 15, y, z & 15);
    }

    public Material getMaterial(int x, int y, int z)
    {
        int var4 = getBlockId(x, y, z);
        return var4 == 0 ? Material.Air : Block.Blocks[var4].material;
    }

    public BiomeSource getBiomeSource() => _world.getBiomeSource();

    public bool isOpaque(int x, int y, int z)
    {
        Block block = Block.Blocks[getBlockId(x, y, z)];
        return block != null && block.isOpaque();
    }

    public bool shouldSuffocate(int x, int y, int z)
    {
        Block block = Block.Blocks[getBlockId(x, y, z)];
        return block != null && block.material.BlocksMovement && block.isFullCube();
    }
}
