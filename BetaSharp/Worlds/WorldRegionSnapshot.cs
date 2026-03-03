using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;

namespace BetaSharp.Worlds;

public class WorldRegionSnapshot : IBlockAccess, IDisposable
{
    private readonly int _chunkX;
    private readonly int _chunkZ;
    private readonly ChunkSnapshot[][] _chunks;
    private readonly float[] _lightTable;
    private readonly int _skylightSubtracted;
    private readonly BiomeSource _biomeSource;
    private bool _isLit;
    private readonly Dictionary<BlockPos, BlockEntity> _tileEntityCache = [];

    public WorldRegionSnapshot(World world, int minX, int var3, int minZ, int maxX, int var6, int maxZ)
    {
        //TODO: OPTIMIZE THIS
        _biomeSource = new(world);

        _chunkX = minX >> 4;
        _chunkZ = minZ >> 4;
        int maxChunkX = maxX >> 4;
        int maxChunkZ = maxZ >> 4;

        int width = maxChunkX - _chunkX + 1;
        int depth = maxChunkZ - _chunkZ + 1;

        _chunks = new ChunkSnapshot[width][];
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i] = new ChunkSnapshot[depth];
        }

        for (int cx = _chunkX; cx <= maxChunkX; ++cx)
        {
            for (int cz = _chunkZ; cz <= maxChunkZ; ++cz)
            {
                Chunk originalChunk = world.GetChunk(cx, cz);
                _chunks[cx - _chunkX][cz - _chunkZ] = new(originalChunk);
            }
        }

        _lightTable = (float[])world.dimension.LightLevelToLuminance.Clone();
        _skylightSubtracted = world.ambientDarkness;
    }

    public int getBlockId(int x, int y, int z)
    {
        if (y is < 0 or >= 128) return 0;

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;

        if (chunkIdxX >= 0 && chunkIdxX < _chunks.Length &&
            chunkIdxZ >= 0 && chunkIdxZ < _chunks[chunkIdxX].Length)
        {
            ChunkSnapshot chunk = _chunks[chunkIdxX][chunkIdxZ];
            return chunk == null ? 0 : chunk.getBlockID(x & 15, y, z & 15);
        }

        return 0;
    }

    public Material getMaterial(int x, int y, int z)
    {
        int blockId = getBlockId(x, y, z);
        return blockId == 0 ? Material.Air : Block.Blocks[blockId].material;
    }

    public int getBlockMeta(int x, int y, int z)
    {
        if (y is < 0 or >= 128) return 0;

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;
        return _chunks[chunkIdxX][chunkIdxZ].getBlockMetadata(x & 15, y, z & 15);
    }

    public BlockEntity? getBlockEntity(int x, int y, int z)
    {
        if (y is < 0 or >= 128) return null;

        var pos = new BlockPos(x, y, z);
        if (_tileEntityCache.TryGetValue(pos, out BlockEntity? entity))
        {
            return entity;
        }

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;

        if (chunkIdxX >= 0 && chunkIdxX < _chunks.Length &&
            chunkIdxZ >= 0 && chunkIdxZ < _chunks[chunkIdxX].Length)
        {
            ChunkSnapshot chunk = _chunks[chunkIdxX][chunkIdxZ];
            if (chunk == null) return null;

            NBTTagCompound? nbt = chunk.GetTileEntityNbt(x & 15, y, z & 15);
            if (nbt != null)
            {
                var newEntity = BlockEntity.CreateFromNbt(nbt);
                if (newEntity != null)
                {
                    _tileEntityCache[pos] = newEntity;
                    return newEntity;
                }
            }
        }

        return null;
    }

    public float getNaturalBrightness(int x, int y, int z, int minLight)
    {
        int light = getLightValue(x, y, z);
        return _lightTable[Math.Max(light, minLight)];
    }

    public float getLuminance(int x, int y, int z)
    {
        return _lightTable[getLightValue(x, y, z)];
    }

    public int getLightValue(int x, int y, int z) => GetLightValueExt(x, y, z, true);

    public int GetLightValueExt(int x, int y, int z, bool checkStairs)
    {
        // World bounds check
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return 15;
        if (checkStairs)
        {
            int blockId = getBlockId(x, y, z);
            if (blockId == Block.Slab.id || blockId == Block.Farmland.id || blockId == Block.WoodenStairs.id || blockId == Block.CobblestoneStairs.id)
            {
                int maxLight = GetLightValueExt(x, y + 1, z, false);
                maxLight = Math.Max(maxLight, GetLightValueExt(x + 1, y, z, false)); // East
                maxLight = Math.Max(maxLight, GetLightValueExt(x - 1, y, z, false)); // West
                maxLight = Math.Max(maxLight, GetLightValueExt(x, y, z + 1, false)); // South
                maxLight = Math.Max(maxLight, GetLightValueExt(x, y, z - 1, false)); // North
                return maxLight;
            }
        }

        if (y < 0) return 0;
        if (y >= 128)
        {
            return Math.Max(0, 15 - _skylightSubtracted);
        }

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;

        ChunkSnapshot chunk = _chunks[chunkIdxX][chunkIdxZ];

        int lightValue = chunk.getBlockLightValue(x & 15, y, z & 15, _skylightSubtracted);

        if (chunk.getIsLit())
        {
            _isLit = true;
        }

        return lightValue;
    }

    public BiomeSource getBiomeSource()
    {
        return _biomeSource;
    }

    public bool shouldSuffocate(int x, int y, int z)
    {
        Block block = Block.Blocks[getBlockId(x, y, z)];
        return block != null && block.material.BlocksMovement && block.isFullCube();
    }

    public bool isOpaque(int x, int y, int z)
    {
        Block block = Block.Blocks[getBlockId(x, y, z)];
        return block != null && block.isOpaque();
    }

    public bool getIsLit()
    {
        return _isLit;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (ChunkSnapshot[] column in _chunks)
        {
            if (column == null) continue;

            foreach (ChunkSnapshot snapshot in column)
            {
                snapshot?.Dispose();
            }
        }
    }
}
