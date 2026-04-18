using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Mechanics;

public class Explosion
{
    private readonly IWorldContext _level;
    private readonly JavaRandom ExplosionRNG = new();
    public HashSet<BlockPos> destroyedBlockPositions = new();
    public Entity? exploder;
    public float explosionSize;
    public double explosionX;
    public double explosionY;
    public double explosionZ;
    public bool isFlaming = false;

    public Explosion(IWorldContext level, Entity? sourceEntity, double x, double y, double z, float size)
    {
        _level = level;
        exploder = sourceEntity;
        explosionSize = size;
        explosionX = x;
        explosionY = y;
        explosionZ = z;
    }

    public void doExplosionA()
    {
        float originalExplosionSize = explosionSize;
        byte rayResolution = 16;

        int rayX;
        int rayY;
        int rayZ;
        double sampleX;
        double sampleY;
        double sampleZ;
        for (rayX = 0; rayX < rayResolution; ++rayX)
        {
            for (rayY = 0; rayY < rayResolution; ++rayY)
            {
                for (rayZ = 0; rayZ < rayResolution; ++rayZ)
                {
                    if (rayX == 0 || rayX == rayResolution - 1 || rayY == 0 || rayY == rayResolution - 1 || rayZ == 0 || rayZ == rayResolution - 1)
                    {
                        double rayDirX = rayX / (rayResolution - 1.0F) * 2.0F - 1.0F;
                        double rayDirY = rayY / (rayResolution - 1.0F) * 2.0F - 1.0F;
                        double rayDirZ = rayZ / (rayResolution - 1.0F) * 2.0F - 1.0F;
                        double rayDirLength = Math.Sqrt(rayDirX * rayDirX + rayDirY * rayDirY + rayDirZ * rayDirZ);
                        rayDirX /= rayDirLength;
                        rayDirY /= rayDirLength;
                        rayDirZ /= rayDirLength;
                        float blastPower = explosionSize * (0.7F + _level.Random.NextFloat() * 0.6F);
                        sampleX = explosionX;
                        sampleY = explosionY;
                        sampleZ = explosionZ;

                        for (float stepSize = 0.3F; blastPower > 0.0F; blastPower -= stepSize * (12.0F / 16.0F))
                        {
                            int blockX = MathHelper.Floor(sampleX);
                            int blockY = MathHelper.Floor(sampleY);
                            int blockZ = MathHelper.Floor(sampleZ);
                            int blockId = _level.Reader.GetBlockId(blockX, blockY, blockZ);
                            if (blockId > 0)
                            {
                                blastPower -= (Block.Blocks[blockId].getBlastResistance(exploder) + 0.3F) * stepSize;
                            }

                            if (blastPower > 0.0F)
                            {
                                destroyedBlockPositions.Add(new BlockPos(blockX, blockY, blockZ));
                            }

                            sampleX += rayDirX * stepSize;
                            sampleY += rayDirY * stepSize;
                            sampleZ += rayDirZ * stepSize;
                        }
                    }
                }
            }
        }

        explosionSize *= 2.0F;
        rayX = MathHelper.Floor(explosionX - explosionSize - 1.0D);
        rayY = MathHelper.Floor(explosionX + explosionSize + 1.0D);
        rayZ = MathHelper.Floor(explosionY - explosionSize - 1.0D);
        int maxY = MathHelper.Floor(explosionY + explosionSize + 1.0D);
        int minZ = MathHelper.Floor(explosionZ - explosionSize - 1.0D);
        int maxZ = MathHelper.Floor(explosionZ + explosionSize + 1.0D);
        List<Entity> affectedEntities = _level.Entities.GetEntities(exploder, new Box(rayX, rayZ, minZ, rayY, maxY, maxZ));
        Vec3D explosionCenter = new(explosionX, explosionY, explosionZ);

        for (int entityIndex = 0; entityIndex < affectedEntities.Count; ++entityIndex)
        {
            Entity entity = affectedEntities[entityIndex];
            double normalizedDistance = entity.GetDistance(explosionX, explosionY, explosionZ) / explosionSize;
            if (normalizedDistance <= 1.0D)
            {
                sampleX = entity.X - explosionX;
                sampleY = entity.Y - explosionY;
                sampleZ = entity.Z - explosionZ;
                double distanceToExplosion = MathHelper.Sqrt(sampleX * sampleX + sampleY * sampleY + sampleZ * sampleZ);
                sampleX /= distanceToExplosion;
                sampleY /= distanceToExplosion;
                sampleZ /= distanceToExplosion;
                double visibilityRatio = _level.Reader.GetVisibilityRatio(explosionCenter, entity.BoundingBox);
                double impact = (1.0D - normalizedDistance) * visibilityRatio;
                entity.Damage(exploder, (int)((impact * impact + impact) / 2.0D * 8.0D * explosionSize + 1.0D));
                entity.VelocityX += sampleX * impact;
                entity.VelocityY += sampleY * impact;
                entity.VelocityZ += sampleZ * impact;
            }
        }

        explosionSize = originalExplosionSize;
        List<BlockPos> destroyedPositions = new(destroyedBlockPositions);
        if (!isFlaming) return;

        for (int positionIndex = destroyedPositions.Count - 1; positionIndex >= 0; --positionIndex)
        {
            BlockPos blockPos = destroyedPositions[positionIndex];
            int x = blockPos.x;
            int y = blockPos.y;
            int z = blockPos.z;
            int blockIdAtPos = _level.Reader.GetBlockId(x, y, z);
            int belowBlockId = _level.Reader.GetBlockId(x, y - 1, z);
            if (blockIdAtPos == 0 && Block.BlocksOpaque[belowBlockId] && ExplosionRNG.NextInt(3) == 0)
            {
                _level.Writer.SetBlock(x, y, z, Block.Fire.id);
            }
        }
    }

    public void doExplosionB(bool spawnParticles)
    {
        _level.Broadcaster.PlaySoundAtPos(explosionX, explosionY, explosionZ, "random.explode", 4.0F, (1.0F + (_level.Random.NextFloat() - _level.Random.NextFloat()) * 0.2F) * 0.7F);
        List<BlockPos> destroyedPositions = new(destroyedBlockPositions);

        for (int positionIndex = destroyedPositions.Count - 1; positionIndex >= 0; --positionIndex)
        {
            BlockPos blockPos = destroyedPositions[positionIndex];
            int x = blockPos.x;
            int y = blockPos.y;
            int z = blockPos.z;
            int blockId = _level.Reader.GetBlockId(x, y, z);
            if (spawnParticles)
            {
                double particleX = x + _level.Random.NextFloat();
                double particleY = y + _level.Random.NextFloat();
                double particleZ = z + _level.Random.NextFloat();
                double particleVelocityX = particleX - explosionX;
                double particleVelocityY = particleY - explosionY;
                double particleVelocityZ = particleZ - explosionZ;
                double distance = MathHelper.Sqrt(particleVelocityX * particleVelocityX + particleVelocityY * particleVelocityY + particleVelocityZ * particleVelocityZ);
                particleVelocityX /= distance;
                particleVelocityY /= distance;
                particleVelocityZ /= distance;
                double velocityScale = 0.5D / (distance / explosionSize + 0.1D);
                velocityScale *= _level.Random.NextFloat() * _level.Random.NextFloat() + 0.3F;
                particleVelocityX *= velocityScale;
                particleVelocityY *= velocityScale;
                particleVelocityZ *= velocityScale;
                _level.Broadcaster.AddParticle("explode", (particleX + explosionX * 1.0D) / 2.0D, (particleY + explosionY * 1.0D) / 2.0D, (particleZ + explosionZ * 1.0D) / 2.0D, particleVelocityX, particleVelocityY, particleVelocityZ);
                _level.Broadcaster.AddParticle("smoke", particleX, particleY, particleZ, particleVelocityX, particleVelocityY, particleVelocityZ);
            }

            if (blockId > 0)
            {
                Block.Blocks[blockId].dropStacks(new OnDropEvent(_level, x, y, z, _level.Reader.GetBlockMeta(x, y, z), 0.3F));
                _level.Writer.SetBlock(x, y, z, 0);
                Block.Blocks[blockId].onDestroyedByExplosion(new OnDestroyedByExplosionEvent(_level, x, y, z));
            }
        }
    }
}
