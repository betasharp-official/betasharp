using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Biomes.Source;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;

namespace BetaSharp.Worlds.Core.Systems;

public class WorldReader : IBlockReader
{
    private readonly IWorldContext _context;
    private readonly Dimension _dimension;

    public WorldReader(IWorldContext context, Dimension dimension)
    {
        _dimension = dimension;
        _context = context;
    }

    public int AmbientDarkness => _context.Environment?.AmbientDarkness ?? 0;

    public bool IsPosLoaded(int x, int y, int z) => y >= 0 && y < ChuckFormat.WorldHeight && _context.ChunkHost.HasChunk(x >> 4, z >> 4);

    public int GetBlockId(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= ChuckFormat.WorldHeight)
        {
            return 0;
        }

        return _context.ChunkHost.GetChunk(x >> 4, z >> 4).GetBlockId(x & 15, y, z & 15);
    }

    public int GetBlockMeta(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= ChuckFormat.WorldHeight)
        {
            return 0;
        }

        return _context.ChunkHost.GetChunk(x >> 4, z >> 4).GetBlockMeta(x & 15, y, z & 15);
    }

    public Material GetMaterial(int x, int y, int z)
    {
        int blockId = GetBlockId(x, y, z);
        return blockId == 0 ? Material.Air : Block.Blocks[blockId].Material;
    }

    public bool IsOpaque(int x, int y, int z)
    {
        Block? block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.IsOpaque();
    }

    public bool ShouldSuffocate(int x, int y, int z)
    {
        if (!IsPosLoaded(x, y, z))
        {
            return false;
        }

        Block? block = Block.Blocks[GetBlockId(x, y, z)];
        return block != null && block.Material.Suffocates && block.IsFullCube();
    }

    public BiomeSource GetBiomeSource() => _dimension.BiomeSource;

    public bool IsAir(int x, int y, int z) => GetBlockId(x, y, z) == 0;

    public int GetBrightness(int x, int y, int z)
    {
        if (y < 0)
        {
            return 0;
        }

        if (y >= ChuckFormat.WorldHeight)
        {
            return !_dimension.HasCeiling ? 15 : 0;
        }

        return _context.ChunkHost.GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, 0);
    }

    public bool IsTopY(int x, int y, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return false;

        if (y < 0)
        {
            return false;
        }

        if (y >= ChuckFormat.WorldHeight)
        {
            return true;
        }

        if (!_context.ChunkHost.HasChunk(x >> 4, z >> 4))
        {
            return false;
        }

        Chunk chunk = _context.ChunkHost.GetChunk(x >> 4, z >> 4);
        return chunk.IsAboveMaxHeight(x & 15, y, z & 15);
    }

    public int GetTopY(int x, int z)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return 0;

        int chunkX = x >> 4;
        int chunkZ = z >> 4;

        if (!_context.ChunkHost.HasChunk(chunkX, chunkZ))
        {
            return 0;
        }

        Chunk chunk = _context.ChunkHost.GetChunk(chunkX, chunkZ);
        return chunk.GetHeight(x & 15, z & 15);

    }

    public int GetTopSolidBlockY(int x, int z)
    {
        Chunk chunk = _context.ChunkHost.GetChunkFromPos(x, z);
        int currentY = ChuckFormat.WorldHeight - 1;
        int localX = x & 15;
        int localZ = z & 15;

        for (; currentY > 0; --currentY)
        {
            int blockId = chunk.GetBlockId(localX, currentY, localZ);
            Material material = blockId == 0 ? Material.Air : Block.Blocks[blockId].Material;

            if (material.BlocksMovement || material.IsFluid)
            {
                return currentY + 1;
            }
        }

        return -1;
    }

    public int GetSpawnPositionValidityY(int x, int z)
    {
        Chunk chunk = _context.ChunkHost.GetChunkFromPos(x, z);
        int currentY = ChuckFormat.WorldHeight - 1;
        int localX = x & 15;
        int localZ = z & 15;

        for (; currentY > 0; currentY--)
        {
            int blockId = chunk.GetBlockId(localX, currentY, localZ);
            if (blockId != 0 && Block.Blocks[blockId].Material.BlocksMovement)
            {
                return currentY + 1;
            }
        }

        return -1;
    }

    public HitResult Raycast(Vec3D start, Vec3D end, bool includeFluids = false, bool ignoreNonSolid = false)
    {
        if (double.IsNaN(start.x) || double.IsNaN(start.y) || double.IsNaN(start.z) ||
            double.IsNaN(end.x) || double.IsNaN(end.y) || double.IsNaN(end.z))
        {
            return new HitResult(HitResultType.MISS);
        }

        int targetX = MathHelper.Floor(end.x);
        int targetY = MathHelper.Floor(end.y);
        int targetZ = MathHelper.Floor(end.z);
        int currentX = MathHelper.Floor(start.x);
        int currentY = MathHelper.Floor(start.y);
        int currentZ = MathHelper.Floor(start.z);

        int initialId = GetBlockId(currentX, currentY, currentZ);
        int initialMeta = GetBlockMeta(currentX, currentY, currentZ);
        Block? initialBlock = Block.Blocks[initialId];

        if ((!ignoreNonSolid || initialBlock == null ||
             initialBlock.GetCollisionShape(this, _context.Entities, currentX, currentY, currentZ) != null) &&
            initialId > 0 && initialBlock!.HasCollision(initialMeta, includeFluids))
        {
            HitResult result = initialBlock.Raycast(this, _context.Entities, currentX, currentY, currentZ, start, end);
            if (result.Type != HitResultType.MISS)
            {
                return result;
            }
        }

        int iterationsRemaining = 200;
        while (iterationsRemaining-- >= 0)
        {
            if (double.IsNaN(start.x) || double.IsNaN(start.y) || double.IsNaN(start.z) || currentX == targetX && currentY == targetY && currentZ == targetZ)
            {
                return new HitResult(HitResultType.MISS);
            }

            bool canMoveX = true, canMoveY = true, canMoveZ = true;
            double nextBoundaryX = 999.0D, nextBoundaryY = 999.0D, nextBoundaryZ = 999.0D;

            if (targetX > currentX)
            {
                nextBoundaryX = currentX + 1.0D;
            }
            else if (targetX < currentX)
            {
                nextBoundaryX = currentX + 0.0D;
            }
            else
            {
                canMoveX = false;
            }

            if (targetY > currentY)
            {
                nextBoundaryY = currentY + 1.0D;
            }
            else if (targetY < currentY)
            {
                nextBoundaryY = currentY + 0.0D;
            }
            else
            {
                canMoveY = false;
            }

            if (targetZ > currentZ)
            {
                nextBoundaryZ = currentZ + 1.0D;
            }
            else if (targetZ < currentZ)
            {
                nextBoundaryZ = currentZ + 0.0D;
            }
            else
            {
                canMoveZ = false;
            }

            double deltaX = end.x - start.x;
            double deltaY = end.y - start.y;
            double deltaZ = end.z - start.z;

            double scaleX = 999.0D, scaleY = 999.0D, scaleZ = 999.0D;
            if (canMoveX)
            {
                scaleX = (nextBoundaryX - start.x) / deltaX;
            }

            if (canMoveY)
            {
                scaleY = (nextBoundaryY - start.y) / deltaY;
            }

            if (canMoveZ)
            {
                scaleZ = (nextBoundaryZ - start.z) / deltaZ;
            }

            byte hitSide;
            if (scaleX < scaleY && scaleX < scaleZ)
            {
                hitSide = (byte)(targetX > currentX ? 4 : 5);
                start.x = nextBoundaryX;
                start.y += deltaY * scaleX;
                start.z += deltaZ * scaleX;
            }
            else if (scaleY < scaleZ)
            {
                hitSide = (byte)(targetY > currentY ? 0 : 1);
                start.x += deltaX * scaleY;
                start.y = nextBoundaryY;
                start.z += deltaZ * scaleY;
            }
            else
            {
                hitSide = (byte)(targetZ > currentZ ? 2 : 3);
                start.x += deltaX * scaleZ;
                start.y += deltaY * scaleZ;
                start.z = nextBoundaryZ;
            }

            Vec3D currentStepPos = new(start.x, start.y, start.z);
            currentX = (int)(currentStepPos.x = MathHelper.Floor(start.x));
            if (hitSide == 5)
            {
                currentX--;
                currentStepPos.x++;
            }

            currentY = (int)(currentStepPos.y = MathHelper.Floor(start.y));
            if (hitSide == 1)
            {
                currentY--;
                currentStepPos.y++;
            }

            currentZ = (int)(currentStepPos.z = MathHelper.Floor(start.z));
            if (hitSide == 3)
            {
                currentZ--;
                currentStepPos.z++;
            }

            int blockIdAtStep = GetBlockId(currentX, currentY, currentZ);
            int metaAtStep = GetBlockMeta(currentX, currentY, currentZ);
            Block? blockAtStep = Block.Blocks[blockIdAtStep];

            if ((!ignoreNonSolid || blockAtStep == null ||
                 blockAtStep.GetCollisionShape(this, _context.Entities, currentX, currentY, currentZ) != null) &&
                blockIdAtStep > 0 && blockAtStep!.HasCollision(metaAtStep, includeFluids))
            {
                HitResult hit = blockAtStep.Raycast(this, _context.Entities, currentX, currentY, currentZ, start, end);
                if (hit.Type != HitResultType.MISS)
                {
                    return hit;
                }
            }
        }

        return new HitResult(HitResultType.MISS);
    }

    public float GetVisibilityRatio(Vec3D sourcePosition, Box targetBox)
    {
        double stepSizeX = 1.0D / ((targetBox.MaxX - targetBox.MinX) * 2.0D + 1.0D);
        double stepSizeY = 1.0D / ((targetBox.MaxY - targetBox.MinY) * 2.0D + 1.0D);
        double stepSizeZ = 1.0D / ((targetBox.MaxZ - targetBox.MinZ) * 2.0D + 1.0D);

        int visiblePoints = 0;
        int totalPoints = 0;

        for (float progressX = 0.0F; progressX <= 1.0F; progressX = (float)(progressX + stepSizeX))
        {
            for (float progressY = 0.0F; progressY <= 1.0F; progressY = (float)(progressY + stepSizeY))
            {
                for (float progressZ = 0.0F; progressZ <= 1.0F; progressZ = (float)(progressZ + stepSizeZ))
                {
                    double sampleX = targetBox.MinX + (targetBox.MaxX - targetBox.MinX) * progressX;
                    double sampleY = targetBox.MinY + (targetBox.MaxY - targetBox.MinY) * progressY;
                    double sampleZ = targetBox.MinZ + (targetBox.MaxZ - targetBox.MinZ) * progressZ;
                    if (Raycast(new Vec3D(sampleX, sampleY, sampleZ), sourcePosition).Type == HitResultType.MISS)
                    {
                        visiblePoints++;
                    }

                    totalPoints++;
                }
            }
        }

        return (float)visiblePoints / totalPoints;
    }

    public bool IsMaterialInBox(Box area, Func<Material, bool> predicate)
    {
        int minX = MathHelper.Floor(area.MinX);
        int maxX = MathHelper.Floor(area.MaxX + 1.0D);
        int minY = MathHelper.Floor(area.MinY);
        int maxY = MathHelper.Floor(area.MaxY + 1.0D);
        int minZ = MathHelper.Floor(area.MinZ);
        int maxZ = MathHelper.Floor(area.MaxZ + 1.0D);

        if (area.MinX < 0.0D)
        {
            minX--;
        }

        if (area.MinY < 0.0D)
        {
            minY--;
        }

        if (area.MinZ < 0.0D)
        {
            minZ--;
        }

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    if (predicate(GetMaterial(x, y, z)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool UpdateMovementInFluid(Box entityBox, Material fluidMaterial, Entity entity)
    {
        int minX = MathHelper.Floor(entityBox.MinX);
        int maxX = MathHelper.Floor(entityBox.MaxX + 1.0D);
        int minY = MathHelper.Floor(entityBox.MinY);
        int maxY = MathHelper.Floor(entityBox.MaxY + 1.0D);
        int minZ = MathHelper.Floor(entityBox.MinZ);
        int maxZ = MathHelper.Floor(entityBox.MaxZ + 1.0D);

        if (!_context.ChunkHost.IsRegionLoaded(minX, minY, minZ, maxX, maxY, maxZ))
        {
            return false;
        }

        bool isSubmerged = false;
        Vec3D flowVector = new(0.0D, 0.0D, 0.0D);

        for (int x = minX; x < maxX; ++x)
        {
            for (int y = minY; y < maxY; ++y)
            {
                for (int z = minZ; z < maxZ; ++z)
                {
                    Block? block = Block.Blocks[GetBlockId(x, y, z)];
                    if (block != null && block.Material == fluidMaterial)
                    {
                        double fluidSurfaceY = y + 1 - BlockFluid.GetFluidHeightFromMeta(GetBlockMeta(x, y, z));

                        if (maxY >= fluidSurfaceY)
                        {
                            isSubmerged = true;
                            Vec3D blockFlow = block.ApplyVelocity(new OnApplyVelocityEvent(_context, entity, x, y, z));
                            flowVector.x += blockFlow.x;
                            flowVector.y += blockFlow.y;
                            flowVector.z += blockFlow.z;
                        }
                    }
                }
            }
        }

        if (flowVector.magnitude() > 0.0D)
        {
            flowVector = flowVector.normalize();
            const double flowStrength = 0.014D;
            entity.VelocityX += flowVector.x * flowStrength;
            entity.VelocityY += flowVector.y * flowStrength;
            entity.VelocityZ += flowVector.z * flowStrength;
        }

        return isSubmerged;
    }

    public void MarkChunkDirty(int x, int z)
    {
        if (_context.Reader.IsPosLoaded(x, 0, z))
        {
            _context.ChunkHost.GetChunkFromPos(x, z).MarkDirty();
        }
    }
}
