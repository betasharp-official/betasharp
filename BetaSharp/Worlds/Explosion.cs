using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Worlds;

public class Explosion
{
    public bool isFlaming = false;
    private JavaRandom ExplosionRNG = new();
    private World worldObj;
    public double explosionX;
    public double explosionY;
    public double explosionZ;
    public Entity exploder;
    public float explosionSize;
    public HashSet<BlockPos> destroyedBlockPositions = new();

    public Explosion(World world, Entity exploder, double x, double y, double z, float size)
    {
        worldObj = world;
        this.exploder = exploder;
        explosionSize = size;
        explosionX = x;
        explosionY = y;
        explosionZ = z;
    }

    public void doExplosionA()
    {
        float savedExplosionSize = explosionSize;
        byte rayGridSize = 16;

        int gridX;
        int gridY;
        int gridZ;
        double rayX;
        double rayY;
        double rayZ;
        for (gridX = 0; gridX < rayGridSize; ++gridX)
        {
            for (gridY = 0; gridY < rayGridSize; ++gridY)
            {
                for (gridZ = 0; gridZ < rayGridSize; ++gridZ)
                {
                    if (gridX == 0 || gridX == rayGridSize - 1 || gridY == 0 || gridY == rayGridSize - 1 || gridZ == 0 || gridZ == rayGridSize - 1)
                    {
                        double dirX = (double)(gridX / (rayGridSize - 1.0F) * 2.0F - 1.0F);
                        double dirY = (double)(gridY / (rayGridSize - 1.0F) * 2.0F - 1.0F);
                        double dirZ = (double)(gridZ / (rayGridSize - 1.0F) * 2.0F - 1.0F);
                        double directionVectorMagnitude = Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
                        dirX /= directionVectorMagnitude;
                        dirY /= directionVectorMagnitude;
                        dirZ /= directionVectorMagnitude;
                        float rayDistance = explosionSize * (0.7F + worldObj.random.NextFloat() * 0.6F);
                        rayX = explosionX;
                        rayY = explosionY;
                        rayZ = explosionZ;

                        for (float rayStepSize = 0.3F; rayDistance > 0.0F; rayDistance -= rayStepSize * (12.0F / 16.0F))
                        {
                            int blockX = MathHelper.Floor(rayX);
                            int blockY = MathHelper.Floor(rayY);
                            int blockZ = MathHelper.Floor(rayZ);
                            int blockId = worldObj.getBlockId(blockX, blockY, blockZ);
                            if (blockId > 0)
                            {
                                rayDistance -= (Block.Blocks[blockId].getBlastResistance(exploder) + 0.3F) * rayStepSize;
                            }

                            if (rayDistance > 0.0F)
                            {
                                destroyedBlockPositions.Add(new BlockPos(blockX, blockY, blockZ));
                            }

                            rayX += dirX * (double)rayStepSize;
                            rayY += dirY * (double)rayStepSize;
                            rayZ += dirZ * (double)rayStepSize;
                        }
                    }
                }
            }
        }

        explosionSize *= 2.0F;
        int minBbX = MathHelper.Floor(explosionX - explosionSize - 1.0D);
        int maxBbX = MathHelper.Floor(explosionX + explosionSize + 1.0D);
        int minBbY = MathHelper.Floor(explosionY - explosionSize - 1.0D);
        int maxBbY = MathHelper.Floor(explosionY + explosionSize + 1.0D);
        int minBbZ = MathHelper.Floor(explosionZ - explosionSize - 1.0D);
        int maxBbZ = MathHelper.Floor(explosionZ + explosionSize + 1.0D);
        var nearbyEntities = worldObj.getEntities(exploder, new Box(minBbX, minBbY, minBbZ, maxBbX, maxBbY, maxBbZ));
        Vec3D explosionCenter = new Vec3D(explosionX, explosionY, explosionZ);

        for (int i = 0; i < nearbyEntities.Count; ++i)
        {
            Entity entity = nearbyEntities[i];
            double normalizedDist = entity.getDistance(explosionX, explosionY, explosionZ) / explosionSize;
            if (normalizedDist <= 1.0D)
            {
                rayX = entity.x - explosionX;
                rayY = entity.y - explosionY;
                rayZ = entity.z - explosionZ;
                double distToExplosion = (double)MathHelper.Sqrt(rayX * rayX + rayY * rayY + rayZ * rayZ);
                rayX /= distToExplosion;
                rayY /= distToExplosion;
                rayZ /= distToExplosion;
                double visibilityRatio = (double)worldObj.getVisibilityRatio(explosionCenter, entity.boundingBox);
                double exposureFactor = (1.0D - normalizedDist) * visibilityRatio;
                entity.damage(exploder, (int)((exposureFactor * exposureFactor + exposureFactor) / 2.0D * 8.0D * explosionSize + 1.0D));
                entity.velocityX += rayX * exposureFactor;
                entity.velocityY += rayY * exposureFactor;
                entity.velocityZ += rayZ * exposureFactor;
            }
        }

        explosionSize = savedExplosionSize;
        List<BlockPos> blocksToProcess = new(destroyedBlockPositions);
        if (isFlaming)
        {
            for (int j = blocksToProcess.Count - 1; j >= 0; --j)
            {
                BlockPos blockPos = blocksToProcess[j];
                int bx = blockPos.x;
                int by = blockPos.y;
                int bz = blockPos.z;
                int blockId = worldObj.getBlockId(bx, by, bz);
                int blockBelowId = worldObj.getBlockId(bx, by - 1, bz);
                if (blockId == 0 && Block.BlocksOpaque[blockBelowId] && ExplosionRNG.NextInt(3) == 0)
                {
                    worldObj.setBlock(bx, by, bz, Block.Fire.id);
                }
            }
        }

    }

    public void doExplosionB(bool spawnParticles)
    {
        worldObj.playSound(explosionX, explosionY, explosionZ, "random.explode", 4.0F, (1.0F + (worldObj.random.NextFloat() - worldObj.random.NextFloat()) * 0.2F) * 0.7F);
        List<BlockPos> blockPositions = new (destroyedBlockPositions);

        for (int i = blockPositions.Count - 1; i >= 0; --i)
        {
            BlockPos blockPos = blockPositions[i];
            int bx = blockPos.x;
            int by = blockPos.y;
            int bz = blockPos.z;
            int blockId = worldObj.getBlockId(bx, by, bz);
            if (spawnParticles)
            {
                double particleX = (double)(bx + worldObj.random.NextFloat());
                double particleY = (double)(by + worldObj.random.NextFloat());
                double particleZ = (double)(bz + worldObj.random.NextFloat());
                double dxToExplosion = particleX - explosionX;
                double dyToExplosion = particleY - explosionY;
                double dzToExplosion = particleZ - explosionZ;
                double distToExplosion = (double)MathHelper.Sqrt(dxToExplosion * dxToExplosion + dyToExplosion * dyToExplosion + dzToExplosion * dzToExplosion);
                dxToExplosion /= distToExplosion;
                dyToExplosion /= distToExplosion;
                dzToExplosion /= distToExplosion;
                double velocityFactor = 0.5D / (distToExplosion / explosionSize + 0.1D);
                velocityFactor *= (double)(worldObj.random.NextFloat() * worldObj.random.NextFloat() + 0.3F);
                dxToExplosion *= velocityFactor;
                dyToExplosion *= velocityFactor;
                dzToExplosion *= velocityFactor;
                worldObj.addParticle("explode", (particleX + explosionX * 1.0D) / 2.0D, (particleY + explosionY * 1.0D) / 2.0D, (particleZ + explosionZ * 1.0D) / 2.0D, dxToExplosion, dyToExplosion, dzToExplosion);
                worldObj.addParticle("smoke", particleX, particleY, particleZ, dxToExplosion, dyToExplosion, dzToExplosion);
            }

            if (blockId > 0)
            {
                Block.Blocks[blockId].dropStacks(worldObj, bx, by, bz, worldObj.getBlockMeta(bx, by, bz), 0.3F);
                worldObj.setBlock(bx, by, bz, 0);
                Block.Blocks[blockId].onDestroyedByExplosion(worldObj, bx, by, bz);
            }
        }

    }
}
