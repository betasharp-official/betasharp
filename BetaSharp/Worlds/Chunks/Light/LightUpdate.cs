using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Chunks.Light;

internal struct LightUpdate
{
    public readonly LightType lightType;

    public int minX;
    public int minY;
    public int minZ;
    public int maxX;
    public int maxY;
    public int maxZ;

    public LightUpdate(LightType lightType, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        this.lightType = lightType;
        this.minX = minX;
        this.minY = minY;
        this.minZ = minZ;
        this.maxX = maxX;
        this.maxY = maxY;
        this.maxZ = maxZ;
    }

    public void updateLight(World world)
    {
        int xSize = maxX - minX + 1;
        int ySize = maxY - minY + 1;
        int zSize = maxZ - minZ + 1;
        int totalBlocks = xSize * ySize * zSize;
        if (totalBlocks > -short.MinValue)
        {
            // _logger.LogInformation("Light too large, skipping!");
        }
        else
        {
            int lastChunkX = 0;
            int lastChunkZ = 0;
            bool chunkChecked = false;
            bool chunkLoaded = false;

            for (int cx = minX; cx <= maxX; ++cx)
            {
                for (int cz = minZ; cz <= maxZ; ++cz)
                {
                    int chunkX = cx >> 4;
                    int chunkZ = cz >> 4;
                    bool isLoaded = false;
                    if (chunkChecked && chunkX == lastChunkX && chunkZ == lastChunkZ)
                    {
                        isLoaded = chunkLoaded;
                    }
                    else
                    {
                        isLoaded = world.isRegionLoaded(cx, 0, cz, 1);
                        if (isLoaded)
                        {
                            Chunk chunk = world.GetChunk(cx >> 4, cz >> 4);
                            if (chunk.IsEmpty())
                            {
                                isLoaded = false;
                            }
                        }

                        chunkLoaded = isLoaded;
                        lastChunkX = chunkX;
                        lastChunkZ = chunkZ;
                    }

                    if (isLoaded)
                    {
                        if (minY < 0)
                        {
                            minY = 0;
                        }

                        if (maxY >= 128)
                        {
                            maxY = 127;
                        }

                        for (int cy = minY; cy <= maxY; ++cy)
                        {
                            int currentLight = world.getBrightness(lightType, cx, cy, cz);
                            int blockId = world.getBlockId(cx, cy, cz);
                            int lightOpacity = Block.BlockLightOpacity[blockId];
                            if (lightOpacity == 0)
                            {
                                lightOpacity = 1;
                            }

                            int sourceLight = 0;
                            if (lightType == LightType.Sky)
                            {
                                if (world.isTopY(cx, cy, cz))
                                {
                                    sourceLight = 15;
                                }
                            }
                            else if (lightType == LightType.Block)
                            {
                                sourceLight = Block.BlocksLightLuminance[blockId];
                            }

                            int newLight;
                            int spreadLight;
                            if (lightOpacity >= 15 && sourceLight == 0)
                            {
                                spreadLight = 0;
                            }
                            else
                            {
                                newLight = world.getBrightness(lightType, cx - 1, cy, cz);
                                int lightEast = world.getBrightness(lightType, cx + 1, cy, cz);
                                int lightDown = world.getBrightness(lightType, cx, cy - 1, cz);
                                int lightUp = world.getBrightness(lightType, cx, cy + 1, cz);
                                int lightNorth = world.getBrightness(lightType, cx, cy, cz - 1);
                                int lightSouth = world.getBrightness(lightType, cx, cy, cz + 1);
                                spreadLight = newLight;
                                if (lightEast > newLight)
                                {
                                    spreadLight = lightEast;
                                }

                                if (lightDown > spreadLight)
                                {
                                    spreadLight = lightDown;
                                }

                                if (lightUp > spreadLight)
                                {
                                    spreadLight = lightUp;
                                }

                                if (lightNorth > spreadLight)
                                {
                                    spreadLight = lightNorth;
                                }

                                if (lightSouth > spreadLight)
                                {
                                    spreadLight = lightSouth;
                                }

                                spreadLight -= lightOpacity;
                                if (spreadLight < 0)
                                {
                                    spreadLight = 0;
                                }

                                if (sourceLight > spreadLight)
                                {
                                    spreadLight = sourceLight;
                                }
                            }

                            if (currentLight != spreadLight)
                            {
                                world.setLight(lightType, cx, cy, cz, spreadLight);
                                newLight = spreadLight - 1;
                                if (newLight < 0)
                                {
                                    newLight = 0;
                                }

                                world.updateLight(lightType, cx - 1, cy, cz, newLight);
                                world.updateLight(lightType, cx, cy - 1, cz, newLight);
                                world.updateLight(lightType, cx, cy, cz - 1, newLight);
                                if (cx + 1 >= maxX)
                                {
                                    world.updateLight(lightType, cx + 1, cy, cz, newLight);
                                }

                                if (cy + 1 >= maxY)
                                {
                                    world.updateLight(lightType, cx, cy + 1, cz, newLight);
                                }

                                if (cz + 1 >= maxZ)
                                {
                                    world.updateLight(lightType, cx, cy, cz + 1, newLight);
                                }
                            }
                        }
                    }
                }
            }

        }
    }

    public bool expand(int newMinX, int newMinY, int newMinZ, int newMaxX, int newMaxY, int newMaxZ)
    {
        if (newMinX >= minX && newMinY >= minY && newMinZ >= minZ && newMaxX <= maxX && newMaxY <= maxY && newMaxZ <= maxZ)
        {
            return true;
        }
        else
        {
            byte expandTolerance = 1;
            if (newMinX >= minX - expandTolerance && newMinY >= minY - expandTolerance && newMinZ >= minZ - expandTolerance && newMaxX <= maxX + expandTolerance && newMaxY <= maxY + expandTolerance && newMaxZ <= maxZ + expandTolerance)
            {
                int oldXSize = maxX - minX;
                int oldYSize = maxY - minY;
                int oldZSize = maxZ - minZ;
                if (newMinX > minX)
                {
                    newMinX = minX;
                }

                if (newMinY > minY)
                {
                    newMinY = minY;
                }

                if (newMinZ > minZ)
                {
                    newMinZ = minZ;
                }

                if (newMaxX < maxX)
                {
                    newMaxX = maxX;
                }

                if (newMaxY < maxY)
                {
                    newMaxY = maxY;
                }

                if (newMaxZ < maxZ)
                {
                    newMaxZ = maxZ;
                }

                int newXSize = newMaxX - newMinX;
                int newYSize = newMaxY - newMinY;
                int newZSize = newMaxZ - newMinZ;
                int oldVolume = oldXSize * oldYSize * oldZSize;
                int newVolume = newXSize * newYSize * newZSize;
                if (newVolume - oldVolume <= 2)
                {
                    minX = newMinX;
                    minY = newMinY;
                    minZ = newMinZ;
                    maxX = newMaxX;
                    maxY = newMaxY;
                    maxZ = newMaxZ;
                    return true;
                }
            }

            return false;
        }
    }
}
