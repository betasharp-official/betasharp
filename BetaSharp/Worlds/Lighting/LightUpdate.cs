using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Worlds.Lighting;

internal struct LightUpdate
{
    public readonly LightType LightType;

    public int MinX;
    public int MinY;
    public int MinZ;
    public int MaxX;
    public int MaxY;
    public int MaxZ;

    public LightUpdate(LightType lightType, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        LightType = lightType;
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }

    public void UpdateLight(IBlockReader reader, ChunkHost host, LightingEngine lighting)
    {
        int sizeX = MaxX - MinX + 1;
        int sizeY = MaxY - MinY + 1;
        int sizeZ = MaxZ - MinZ + 1;
        int updateVolume = sizeX * sizeY * sizeZ;

        if (updateVolume > -short.MinValue)
        {
            // _logger.LogInformation("Light too large, skipping!");
            return;
        }

        int startY = MinY < 0 ? 0 : MinY;
        int endY = MaxY >= ChuckFormat.WorldHeight ? ChuckFormat.WorldHeight - 1 : MaxY;

        int cachedChunkX = 0;
        int cachedChunkZ = 0;
        bool isCacheValid = false;
        bool isCachedChunkLoaded = false;

        for (int x = MinX; x <= MaxX; ++x)
        {
            for (int z = MinZ; z <= MaxZ; ++z)
            {
                int chunkX = x >> 4;
                int chunkZ = z >> 4;
                bool isChunkLoaded;

                if (isCacheValid && chunkX == cachedChunkX && chunkZ == cachedChunkZ)
                {
                    isChunkLoaded = isCachedChunkLoaded;
                }
                else
                {
                    isChunkLoaded = host.IsRegionLoaded(x, 0, z, 1);
                    if (isChunkLoaded)
                    {
                        Chunk chunk = host.GetChunk(chunkX, chunkZ);
                        if (chunk.IsEmpty())
                        {
                            isChunkLoaded = false;
                        }
                    }

                    isCachedChunkLoaded = isChunkLoaded;
                    cachedChunkX = chunkX;
                    cachedChunkZ = chunkZ;
                    isCacheValid = true;
                }

                if (isChunkLoaded)
                {
                    for (int y = startY; y <= endY; ++y)
                    {
                        int currentLight = lighting.GetBrightness(LightType, x, y, z);
                        int blockId = reader.GetBlockId(x, y, z);

                        int opacity = Block.BlockLightOpacity[blockId];
                        if (opacity == 0)
                        {
                            opacity = 1;
                        }

                        int emittedLight = 0;
                        if (LightType == LightType.Sky)
                        {
                            if (reader.IsTopY(x, y, z))
                            {
                                emittedLight = 15;
                            }
                        }
                        else if (LightType == LightType.Block)
                        {
                            emittedLight = Block.BlocksLightLuminance[blockId];
                        }

                        int targetLight;
                        if (opacity >= 15 && emittedLight == 0)
                        {
                            targetLight = 0;
                        }
                        else
                        {
                            int westLight = lighting.GetBrightness(LightType, x - 1, y, z);
                            int eastLight = lighting.GetBrightness(LightType, x + 1, y, z);
                            int downLight = lighting.GetBrightness(LightType, x, y - 1, z);
                            int upLight = lighting.GetBrightness(LightType, x, y + 1, z);
                            int northLight = lighting.GetBrightness(LightType, x, y, z - 1);
                            int southLight = lighting.GetBrightness(LightType, x, y, z + 1);

                            targetLight = westLight;
                            if (eastLight > targetLight)
                            {
                                targetLight = eastLight;
                            }

                            if (downLight > targetLight)
                            {
                                targetLight = downLight;
                            }

                            if (upLight > targetLight)
                            {
                                targetLight = upLight;
                            }

                            if (northLight > targetLight)
                            {
                                targetLight = northLight;
                            }

                            if (southLight > targetLight)
                            {
                                targetLight = southLight;
                            }

                            targetLight -= opacity;
                            if (targetLight < 0)
                            {
                                targetLight = 0;
                            }

                            if (emittedLight > targetLight)
                            {
                                targetLight = emittedLight;
                            }
                        }

                        if (currentLight != targetLight)
                        {
                            lighting.SetLight(LightType, x, y, z, targetLight);

                            int prop = targetLight - 1;
                            if (prop < 0)
                            {
                                prop = 0;
                            }

                            lighting.UpdateLight(LightType, x - 1, y, z, prop);
                            lighting.UpdateLight(LightType, x, y - 1, z, prop);
                            lighting.UpdateLight(LightType, x, y, z - 1, prop);

                            if (x + 1 >= MaxX)
                            {
                                lighting.UpdateLight(LightType, x + 1, y, z, prop);
                            }

                            if (y + 1 >= MaxY)
                            {
                                lighting.UpdateLight(LightType, x, y + 1, z, prop);
                            }

                            if (z + 1 >= MaxZ)
                            {
                                lighting.UpdateLight(LightType, x, y, z + 1, prop);
                            }
                        }
                    }
                }
            }
        }
    }

    public bool Expand(int reqMinX, int reqMinY, int reqMinZ, int reqMaxX, int reqMaxY, int reqMaxZ)
    {
        if (reqMinX >= MinX && reqMinY >= MinY && reqMinZ >= MinZ &&
            reqMaxX <= MaxX && reqMaxY <= MaxY && reqMaxZ <= MaxZ)
        {
            return true;
        }

        byte expandTolerance = 1;

        if (reqMinX >= MinX - expandTolerance && reqMinY >= MinY - expandTolerance && reqMinZ >= MinZ - expandTolerance &&
            reqMaxX <= MaxX + expandTolerance && reqMaxY <= MaxY + expandTolerance && reqMaxZ <= MaxZ + expandTolerance)
        {
            int oldVolumeX = MaxX - MinX;
            int oldVolumeY = MaxY - MinY;
            int oldVolumeZ = MaxZ - MinZ;

            int newMinX = reqMinX > MinX ? MinX : reqMinX;
            int newMinY = reqMinY > MinY ? MinY : reqMinY;
            int newMinZ = reqMinZ > MinZ ? MinZ : reqMinZ;
            int newMaxX = reqMaxX < MaxX ? MaxX : reqMaxX;
            int newMaxY = reqMaxY < MaxY ? MaxY : reqMaxY;
            int newMaxZ = reqMaxZ < MaxZ ? MaxZ : reqMaxZ;

            int newVolumeX = newMaxX - newMinX;
            int newVolumeY = newMaxY - newMinY;
            int newVolumeZ = newMaxZ - newMinZ;

            int oldVolume = oldVolumeX * oldVolumeY * oldVolumeZ;
            int newVolume = newVolumeX * newVolumeY * newVolumeZ;

            if (newVolume - oldVolume <= 2)
            {
                MinX = newMinX;
                MinY = newMinY;
                MinZ = newMinZ;
                MaxX = newMaxX;
                MaxY = newMaxY;
                MaxZ = newMaxZ;
                return true;
            }
        }

        return false;
    }
}
