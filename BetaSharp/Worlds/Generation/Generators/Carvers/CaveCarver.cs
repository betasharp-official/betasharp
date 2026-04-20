using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Generation.Generators.Carvers;

internal class CaveCarver : Carver
{
    protected void CarveCavesInChunk(int chunkX, int chunkZ, byte[] blocks, double offsetX, double offsetY, double offsetZ) =>
        CarveCaves(chunkX, chunkZ, blocks, offsetX, offsetY, offsetZ, 1.0F + Rand.NextFloat() * 6.0F, 0.0F, 0.0F, -1, -1, 0.5D);

    protected void CarveCaves(int chunkX, int chunkZ, byte[] blocks, double offsetX, double offsetY, double offsetZ, float tunnelRadius, float yaw, float pitch, int tunnelStep, int tunnelLength, double verticalScale)
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
            offsetX += MathHelper.Cos(yaw) * cosPitch;
            offsetY += sinPitch;
            offsetZ += MathHelper.Sin(yaw) * cosPitch;
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
                CarveCaves(chunkX, chunkZ, blocks, offsetX, offsetY, offsetZ, caveRand.NextFloat() * 0.5F + 0.5F, yaw - (float)Math.PI * 0.5F, pitch / 3.0F, tunnelStep, tunnelLength, 1.0D);
                CarveCaves(chunkX, chunkZ, blocks, offsetX, offsetY, offsetZ, caveRand.NextFloat() * 0.5F + 0.5F, yaw + (float)Math.PI * 0.5F, pitch / 3.0F, tunnelStep, tunnelLength, 1.0D);
                return;
            }

            if (isStartingPoint || caveRand.NextInt(4) != 0)
            {
                double distX = offsetX - chunkCenterX;
                double distZ = offsetZ - chunkCenterZ;
                double stepsRemaining = tunnelLength - tunnelStep;
                double boundRadius = tunnelRadius + 2.0F + 16.0F;
                if (distX * distX + distZ * distZ - stepsRemaining * stepsRemaining > boundRadius * boundRadius)
                {
                    return;
                }

                if (offsetX >= chunkCenterX - 16.0D - horizontalRadius * 2.0D && offsetZ >= chunkCenterZ - 16.0D - horizontalRadius * 2.0D && offsetX <= chunkCenterX + 16.0D + horizontalRadius * 2.0D &&
                    offsetZ <= chunkCenterZ + 16.0D + horizontalRadius * 2.0D)
                {
                    int xMin = MathHelper.Floor(offsetX - horizontalRadius) - chunkX * 16 - 1;
                    int xMax = MathHelper.Floor(offsetX + horizontalRadius) - chunkX * 16 + 1;
                    int yMin = MathHelper.Floor(offsetY - verticalRadius) - 1;
                    int yMax = MathHelper.Floor(offsetY + verticalRadius) + 1;
                    int zMin = MathHelper.Floor(offsetZ - horizontalRadius) - chunkZ * 16 - 1;
                    int zMax = MathHelper.Floor(offsetZ + horizontalRadius) - chunkZ * 16 + 1;
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

                    bool waterIsPresent = false;

                    for (int blockX = xMin; !waterIsPresent && blockX < xMax; ++blockX)
                    {
                        for (int blockZ = zMin; !waterIsPresent && blockZ < zMax; ++blockZ)
                        {
                            for (int blockY = yMax + 1; !waterIsPresent && blockY >= yMin - 1; --blockY)
                            {
                                int blockIndex = (blockX * 16 + blockZ) * 128 + blockY;
                                if (blockY >= 0 && blockY < 128)
                                {
                                    if (blocks[blockIndex] == Block.FlowingWater.ID || blocks[blockIndex] == Block.Water.ID)
                                    {
                                        waterIsPresent = true;
                                    }

                                    if (blockY != yMin - 1 && blockX != xMin && blockX != xMax - 1 && blockZ != zMin && blockZ != zMax - 1)
                                    {
                                        blockY = yMin;
                                    }
                                }
                            }
                        }
                    }

                    if (!waterIsPresent)
                    {
                        for (int blockX = xMin; blockX < xMax; ++blockX)
                        {
                            double localX = (blockX + chunkX * 16 + 0.5D - offsetX) / horizontalRadius;

                            for (int blockZ = zMin; blockZ < zMax; ++blockZ)
                            {
                                double localZ = (blockZ + chunkZ * 16 + 0.5D - offsetZ) / horizontalRadius;
                                int blockIndex = (blockX * 16 + blockZ) * 128 + yMax;
                                bool isGrassBlock = false;
                                if (localX * localX + localZ * localZ < 1.0D)
                                {
                                    for (int blockY = yMax - 1; blockY >= yMin; --blockY)
                                    {
                                        double localY = (blockY + 0.5D - offsetY) / verticalRadius;
                                        if (localY > -0.7D && localX * localX + localY * localY + localZ * localZ < 1.0D)
                                        {
                                            byte blockType = blocks[blockIndex];
                                            if (blockType == Block.GrassBlock.ID)
                                            {
                                                isGrassBlock = true;
                                            }

                                            if (blockType == Block.Stone.ID || blockType == Block.Dirt.ID || blockType == Block.GrassBlock.ID)
                                            {
                                                if (blockY < 10)
                                                {
                                                    blocks[blockIndex] = (byte)Block.FlowingLava.ID;
                                                }
                                                else
                                                {
                                                    blocks[blockIndex] = 0;
                                                    if (isGrassBlock && blocks[blockIndex - 1] == Block.Dirt.ID)
                                                    {
                                                        blocks[blockIndex - 1] = (byte)Block.GrassBlock.ID;
                                                    }
                                                }
                                            }
                                        }

                                        --blockIndex;
                                    }
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
        int numCaves = Rand.NextInt(Rand.NextInt(Rand.NextInt(40) + 1) + 1);
        if (Rand.NextInt(15) != 0)
        {
            numCaves = 0;
        }

        for (int i = 0; i < numCaves; ++i)
        {
            double caveX = chunkX * 16 + Rand.NextInt(16);
            double caveY = Rand.NextInt(Rand.NextInt(120) + 8);
            double caveZ = chunkZ * 16 + Rand.NextInt(16);
            int branchCount = 1;
            if (Rand.NextInt(4) == 0)
            {
                CarveCavesInChunk(centerChunkX, centerChunkZ, blocks, caveX, caveY, caveZ);
                branchCount += Rand.NextInt(4);
            }

            for (int branch = 0; branch < branchCount; ++branch)
            {
                float yaw = Rand.NextFloat() * (float)Math.PI * 2.0F;
                float pitch = (Rand.NextFloat() - 0.5F) * 2.0F / 8.0F;
                float tunnelRadius = Rand.NextFloat() * 2.0F + Rand.NextFloat();
                CarveCaves(centerChunkX, centerChunkZ, blocks, caveX, caveY, caveZ, tunnelRadius, yaw, pitch, 0, 0, 1.0D);
            }
        }
    }
}
