using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Generation.Biomes.Source;

namespace BetaSharp.Worlds.Core.Systems;

internal class WorldRegion : IBlockReader
{
    private readonly Chunk[][] _chunks;
    private readonly int _chunkX;
    private readonly int _chunkZ;
    private readonly IWorldContext _level;

    public WorldRegion(IWorldContext level, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        _level = level;
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
                _chunks[cx - _chunkX][cz - _chunkZ] = level.BlockHost.GetChunk(cx, cz);
            }
        }
    }

    public int GetBlockId(int x, int y, int z)
    {
        if (y is < 0 or >= 128)
        {
            return 0;
        }

        int cx = (x >> 4) - _chunkX;
        int cz = (z >> 4) - _chunkZ;

        if (cx >= 0 && cx < _chunks.Length && cz >= 0 && cz < _chunks[cx].Length)
        {
            Chunk chunk = _chunks[cx][cz];
            return chunk?.GetBlockId(x & 15, y, z & 15) ?? 0;
        }

        return 0;
    }

    public BlockEntity? GetBlockEntity(int x, int y, int z)
    {
        int cx = (x >> 4) - _chunkX;
        int cz = (z >> 4) - _chunkZ;

        if (cx < 0 || cx >= _chunks.Length || cz < 0 || cz < 0 || cz >= _chunks[cx].Length)
        {
            return null;
        }

        return _chunks[cx][cz]?.GetBlockEntity(x & 15, y, z & 15);
    }

    public BiomeSource GetBiomeSource() => _level.Reader.GetBiomeSource();

    public bool IsOpaque(int x, int y, int z)
    {
        Block block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.isOpaque();
    }

    public bool ShouldSuffocate(int x, int y, int z)
    {
        Block block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.material.Suffocates && block.isFullCube();
    }

    public int GetBlockMeta(int x, int y, int z) => throw new NotImplementedException();
    public Material GetMaterial(int x, int y, int z) => throw new NotImplementedException();
    public bool IsAir(int x, int y, int z) => throw new NotImplementedException();
    public int GetBrightness(int x, int y, int z) => throw new NotImplementedException();
    public bool IsTopY(int x, int y, int z) => throw new NotImplementedException();
    public int GetTopY(int x, int z) => throw new NotImplementedException();
    public int GetTopSolidBlockY(int x, int z) => throw new NotImplementedException();
    public int GetSpawnPositionValidityY(int x, int z) => throw new NotImplementedException();
    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox) => throw new NotImplementedException();
    public HitResult Raycast(Vec3D start, Vec3D end) => throw new NotImplementedException();
    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids) => throw new NotImplementedException();
    public HitResult Raycast(Vec3D start, Vec3D target, bool includeFluids, bool ignoreNonSolid) => throw new NotImplementedException();
    public bool IsAnyBlockInBox(Box area) => throw new NotImplementedException();
    public bool IsBoxSubmergedInFluid(Box area) => throw new NotImplementedException();
    public bool IsFireOrLavaInBox(Box area) => throw new NotImplementedException();
    public bool IsMaterialInBox(Box area, Material material) => throw new NotImplementedException();
    public bool IsFluidInBox(Box area, Material fluid) => throw new NotImplementedException();
    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity) => throw new NotImplementedException();

    public bool IsPosLoaded(int x, int y, int z) => throw new NotImplementedException();

    public float GetNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int finalLight = Math.Max(getRawBrightness(x, y, z), blockLight);
        return _level.dimension.LightLevelToLuminance[finalLight];
    }

    public float GetLuminance(int x, int y, int z) => _level.dimension.LightLevelToLuminance[getRawBrightness(x, y, z)];

    public int getBlockMeta(int x, int y, int z)
    {
        if (y is < 0 or >= 128)
        {
            return 0;
        }

        int cx = (x >> 4) - _chunkX;
        int cz = (z >> 4) - _chunkZ;
        return _chunks[cx][cz].GetBlockMeta(x & 15, y, z & 15);
    }

    public Material getMaterial(int x, int y, int z)
    {
        int var4 = GetBlockId(x, y, z);
        return var4 == 0 ? Material.Air : Block.Blocks[var4].material;
    }

    public int getRawBrightness(int x, int y, int z) => getRawBrightness(x, y, z, true);

    public int getRawBrightness(int x, int y, int z, bool useNeighborLight)
    {
        // World bounds check
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000)
        {
            return 15;
        }

        if (useNeighborLight)
        {
            int id = GetBlockId(x, y, z);
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

        if (y < 0)
        {
            return 0;
        }

        if (y >= 128)
        {
            return Math.Max(0, 15 - _level.Environment.AmbientDarkness);
        }

        int cIdxX = (x >> 4) - _chunkX;
        int cIdxZ = (z >> 4) - _chunkZ;

        return _chunks[cIdxX][cIdxZ].GetLight(x & 15, y, z & 15, _level.Environment.AmbientDarkness);
    }
}
