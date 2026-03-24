using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Carvers;

internal class NetherCaveCarver : Carver
{
    protected void CarveNetherCavesInChunk(int chunkX, int chunkZ, byte[] blocks, double x, double y, double z) => CarveNetherCaves(chunkX, chunkZ, blocks, x, y, z, 1.0F + Rand.NextFloat() * 6.0F, 0.0F, 0.0F, -1, -1, 0.5D);

    protected void CarveNetherCaves(int chunkX, int chunkZ, byte[] blocks, double x, double y, double z, float tunnelRadius, float yaw, float pitch, int tunnelStep, int tunnelLength, double verticalScale)
    {
        double chunkCenterX = chunkX * 16 + 8;
        double chunkCenterZ = chunkZ * 16 + 8;
        float yawSpeed = 0.0F;
        float pitchSpeed = 0.0F;
        JavaRandom caveRand = new(Rand.NextLong());
        if (tunnelLength <= 0)
        {
            int range = Radius * 16 - 16;
            tunnelLength = range - caveRand.NextInt(range / 4);
        }

        bool isStartingPoint = false;
        if (tunnelStep == -1)
        {
            tunnelStep = tunnelLength / 2;
            isStartingPoint = true;
        }

        int branchStep = caveRand.NextInt(tunnelLength / 2) + tunnelLength / 4;

        for (bool isLargeRoom = caveRand.NextInt(6) == 0; tunnelStep < tunnelLength; ++tunnelStep)
        {
            double horizontalRadius = 1.5D + MathHelper.Sin(tunnelStep * (float)Math.PI / tunnelLength) * tunnelRadius * 1.0F;
            double verticalRadius = horizontalRadius * verticalScale;
            float cosPitch = MathHelper.Cos(pitch);
            float sinPitch = MathHelper.Sin(pitch);
            x += MathHelper.Cos(yaw) * cosPitch;
            y += sinPitch;
            z += MathHelper.Sin(yaw) * cosPitch;
            if (isLargeRoom)
            {
                pitch *= 0.92F;
            }
            else
            {
                pitch *= 0.7F;
            }

            pitch += pitchSpeed * 0.1F;
            yaw += yawSpeed * 0.1F;
            pitchSpeed *= 0.9F;
            yawSpeed *= 12.0F / 16.0F;
            pitchSpeed += (caveRand.NextFloat() - caveRand.NextFloat()) * caveRand.NextFloat() * 2.0F;
            yawSpeed += (caveRand.NextFloat() - caveRand.NextFloat()) * caveRand.NextFloat() * 4.0F;
            if (!isStartingPoint && tunnelStep == branchStep && tunnelRadius > 1.0F)
            {
                CarveNetherCaves(chunkX, chunkZ, blocks, x, y, z, caveRand.NextFloat() * 0.5F + 0.5F, yaw - (float)Math.PI * 0.5F, pitch / 3.0F, tunnelStep, tunnelLength, 1.0D);
                CarveNetherCaves(chunkX, chunkZ, blocks, x, y, z, caveRand.NextFloat() * 0.5F + 0.5F, yaw + (float)Math.PI * 0.5F, pitch / 3.0F, tunnelStep, tunnelLength, 1.0D);
                return;
            }

            if (isStartingPoint || caveRand.NextInt(4) != 0)
            {
                double distX = x - chunkCenterX;
                double distZ = z - chunkCenterZ;
                double stepsRemaining = tunnelLength - tunnelStep;
                double boundRadius = tunnelRadius + 2.0F + 16.0F;
                if (distX * distX + distZ * distZ - stepsRemaining * stepsRemaining > boundRadius * boundRadius)
                {
                    return;
                }

                if (x >= chunkCenterX - 16.0D - horizontalRadius * 2.0D && z >= chunkCenterZ - 16.0D - horizontalRadius * 2.0D && x <= chunkCenterX + 16.0D + horizontalRadius * 2.0D && z <= chunkCenterZ + 16.0D + horizontalRadius * 2.0D)
                {
                    int xMin = MathHelper.Floor(x - horizontalRadius) - chunkX * 16 - 1;
                    int xMax = MathHelper.Floor(x + horizontalRadius) - chunkX * 16 + 1;
                    int yMin = MathHelper.Floor(y - verticalRadius) - 1;
                    int yMax = MathHelper.Floor(y + verticalRadius) + 1;
                    int zMin = MathHelper.Floor(z - horizontalRadius) - chunkZ * 16 - 1;
                    int zMax = MathHelper.Floor(z + horizontalRadius) - chunkZ * 16 + 1;
                    if (xMin < 0)
                    {
                        xMin = 0;
                    }

                    if (xMax > 16)
                    {
                        xMax = 16;
                    }

                    if (yMin < 1)
                    {
                        yMin = 1;
                    }

                    if (yMax > 120)
                    {
                        yMax = 120;
                    }

                    if (zMin < 0)
                    {
                        zMin = 0;
                    }

                    if (zMax > 16)
                    {
                        zMax = 16;
                    }

                    bool lavaIsPresent = false;

                    int blockX;
                    int indexOrBlockZ;
                    for (blockX = xMin; !lavaIsPresent && blockX < xMax; ++blockX)
                    {
                        for (int blockZ = zMin; !lavaIsPresent && blockZ < zMax; ++blockZ)
                        {
                            for (int blockY = yMax + 1; !lavaIsPresent && blockY >= yMin - 1; --blockY)
                            {
                                indexOrBlockZ = (blockX * 16 + blockZ) * 128 + blockY;
                                if (blockY >= 0 && blockY < 128)
                                {
                                    if (blocks[indexOrBlockZ] == Block.FlowingLava.id || blocks[indexOrBlockZ] == Block.Lava.id)
                                    {
                                        lavaIsPresent = true;
                                    }

                                    if (blockY != yMin - 1 && blockX != xMin && blockX != xMax - 1 && blockZ != zMin && blockZ != zMax - 1)
                                    {
                                        blockY = yMin;
                                    }
                                }
                            }
                        }
                    }

                    if (!lavaIsPresent)
                    {
                        for (blockX = xMin; blockX < xMax; ++blockX)
                        {
                            double localX = (blockX + chunkX * 16 + 0.5D - x) / horizontalRadius;

                            for (indexOrBlockZ = zMin; indexOrBlockZ < zMax; ++indexOrBlockZ)
                            {
                                double localZ = (indexOrBlockZ + chunkZ * 16 + 0.5D - z) / horizontalRadius;
                                int blockIndex = (blockX * 16 + indexOrBlockZ) * 128 + yMax;

                                for (int blockY = yMax - 1; blockY >= yMin; --blockY)
                                {
                                    double localY = (blockY + 0.5D - y) / verticalRadius;
                                    if (localY > -0.7D && localX * localX + localY * localY + localZ * localZ < 1.0D)
                                    {
                                        byte blockType = blocks[blockIndex];
                                        if (blockType == Block.Netherrack.id || blockType == Block.Dirt.id || blockType == Block.GrassBlock.id)
                                        {
                                            blocks[blockIndex] = 0;
                                        }
                                    }

                                    --blockIndex;
                                }
                            }
                        }

                        if (isStartingPoint)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    protected override void CarveCaves(IWorldContext world, int chunkX, int chunkZ, int centerChunkX, int centerChunkZ, byte[] blocks)
    {
        int numCaves = Rand.NextInt(Rand.NextInt(Rand.NextInt(10) + 1) + 1);
        if (Rand.NextInt(5) != 0)
        {
            numCaves = 0;
        }

        for (int i = 0; i < numCaves; ++i)
        {
            double randX = chunkX * 16 + Rand.NextInt(16);
            double randY = Rand.NextInt(128);
            double randZ = chunkZ * 16 + Rand.NextInt(16);
            int branchCount = 1;
            if (Rand.NextInt(4) == 0)
            {
                CarveNetherCavesInChunk(centerChunkX, centerChunkZ, blocks, randX, randY, randZ);
                branchCount += Rand.NextInt(4);
            }

            for (int branch = 0; branch < branchCount; ++branch)
            {
                float yaw = Rand.NextFloat() * (float)Math.PI * 2.0F;
                float pitch = (Rand.NextFloat() - 0.5F) * 2.0F / 8.0F;
                float tunnelRadius = Rand.NextFloat() * 2.0F + Rand.NextFloat();
                CarveNetherCaves(centerChunkX, centerChunkZ, blocks, randX, randY, randZ, tunnelRadius * 2.0F, yaw, pitch, 0, 0, 0.5D);
            }
        }
    }
}
