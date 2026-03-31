using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Core;

public class WorldRegionSnapshot : IBlockReader, ILightProvider, IDisposable
{
    private readonly BiomeSource _biomeSource;
    private readonly ChunkSnapshot[,] _chunks;
    private readonly int _chunkX;
    private readonly int _chunkZ;
    private readonly float[] _lightTable;
    private readonly int _skylightSubtracted;
    private readonly Dictionary<BlockPos, BlockEntity> _tileEntityCache = [];
    private bool _isLit;

    public WorldRegionSnapshot(IWorldContext world, int minX, int var3, int minZ, int maxX, int var6, int maxZ)
    {
        if (!BiomeSource.Pool.TryTake(out _biomeSource!))
        {
            _biomeSource = new(world);
        }
        else
        {
            _biomeSource.Restore(world);
        }

        _chunkX = minX >> 4;
        _chunkZ = minZ >> 4;
        int maxChunkX = maxX >> 4;
        int maxChunkZ = maxZ >> 4;

        int width = maxChunkX - _chunkX + 1;
        int depth = maxChunkZ - _chunkZ + 1;

        _chunks = new ChunkSnapshot[width, depth];

        for (int cx = _chunkX; cx <= maxChunkX; ++cx)
        {
            for (int cz = _chunkZ; cz <= maxChunkZ; ++cz)
            {
                Chunk originalChunk = world.ChunkHost.GetChunk(cx, cz);
                _chunks[cx - _chunkX, cz - _chunkZ] = new ChunkSnapshot(originalChunk);
            }
        }

        _lightTable = world.Dimension.LightLevelToLuminance;
        _skylightSubtracted = world.Environment.AmbientDarkness;
    }

    public int GetBlockId(int x, int y, int z)
    {
        if (y is < 0 or >= 128)
        {
            return 0;
        }

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;

        if (chunkIdxX >= 0 && chunkIdxX < _chunks.GetLength(0) &&
            chunkIdxZ >= 0 && chunkIdxZ < _chunks.GetLength(1))
        {
            ChunkSnapshot chunk = _chunks[chunkIdxX, chunkIdxZ];
            return chunk.getBlockID(x & 15, y, z & 15);
        }

        return 0;
    }

    public BlockEntity? GetBlockEntity(int x, int y, int z)
    {
        if (y is < 0 or >= 128)
        {
            return null;
        }

        BlockPos pos = new(x, y, z);
        if (_tileEntityCache.TryGetValue(pos, out BlockEntity? entity))
        {
            return entity;
        }

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;

        if (chunkIdxX >= 0 && chunkIdxX < _chunks.GetLength(0) &&
            chunkIdxZ >= 0 && chunkIdxZ < _chunks.GetLength(1))
        {
            ChunkSnapshot chunk = _chunks[chunkIdxX, chunkIdxZ];

            NBTTagCompound? nbt = chunk.GetTileEntityNbt(x & 15, y, z & 15);
            if (nbt != null)
            {
                BlockEntity? newEntity = BlockEntity.CreateFromNbt(nbt);
                if (newEntity != null)
                {
                    _tileEntityCache[pos] = newEntity;
                    return newEntity;
                }
            }
        }

        return null;
    }

    public BiomeSource GetBiomeSource() => _biomeSource;

    public bool ShouldSuffocate(int x, int y, int z)
    {
        Block block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.material.BlocksMovement && block.isFullCube();
    }

    public bool IsOpaque(int x, int y, int z)
    {
        Block block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.isOpaque();
    }

    public int GetBlockMeta(int x, int y, int z) => getBlockMeta(x, y, z);
    public Material GetMaterial(int x, int y, int z) => getMaterial(x, y, z);
    public bool IsAir(int x, int y, int z) => GetBlockId(x, y, z) == 0;
    public int GetBrightness(int x, int y, int z) => getLightValue(x, y, z);
    public bool IsTopY(int x, int y, int z) => throw new NotImplementedException();
    public int GetTopY(int x, int z) => throw new NotImplementedException();
    public int GetTopSolidBlockY(int x, int z) => throw new NotImplementedException();
    public int GetSpawnPositionValidityY(int x, int z) => throw new NotImplementedException();
    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox) => throw new NotImplementedException();
    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false) => throw new NotImplementedException();
    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate) => throw new NotImplementedException();
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity) => throw new NotImplementedException();
    public bool IsPosLoaded(int x, int y, int z) => throw new NotImplementedException();

    public float GetNaturalBrightness(int x, int y, int z, int minLight)
    {
        int light = getLightValue(x, y, z);
        return _lightTable[Math.Max(light, minLight)];
    }

    public float GetLuminance(int x, int y, int z) => _lightTable[getLightValue(x, y, z)];

    public Material getMaterial(int x, int y, int z)
    {
        int blockId = GetBlockId(x, y, z);
        return blockId == 0 ? Material.Air : Block.Blocks[blockId].material;
    }

    public int getBlockMeta(int x, int y, int z)
    {
        if (y is < 0 or >= 128)
        {
            return 0;
        }

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;
        return _chunks[chunkIdxX, chunkIdxZ].getBlockMetadata(x & 15, y, z & 15);
    }

    public int getLightValue(int x, int y, int z) => GetLightValueExt(x, y, z, true);

    public int GetLightValueExt(int x, int y, int z, bool checkStairs)
    {
        // World bounds check
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000)
        {
            return 15;
        }

        if (checkStairs)
        {
            int blockId = GetBlockId(x, y, z);
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

        if (y < 0)
        {
            return 0;
        }

        if (y >= 128)
        {
            return Math.Max(0, 15 - _skylightSubtracted);
        }

        int chunkIdxX = (x >> 4) - _chunkX;
        int chunkIdxZ = (z >> 4) - _chunkZ;

        ChunkSnapshot chunk = _chunks[chunkIdxX, chunkIdxZ];

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
        Block block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.material.BlocksMovement && block.isFullCube();
    }

    public bool isOpaque(int x, int y, int z)
    {
        Block block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.isOpaque();
    }

    public bool getIsLit()
    {
        return _isLit;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_biomeSource != null)
        {
            BiomeSource.Pool.Add(_biomeSource);
        }

        foreach (var snapshot in _chunks)
        {
            snapshot.Dispose();
        }
    }

    ~WorldRegionSnapshot()
    {
        Dispose();
    }

    public void MarkChunkDirty(int x, int z) => throw new NotImplementedException();
}
