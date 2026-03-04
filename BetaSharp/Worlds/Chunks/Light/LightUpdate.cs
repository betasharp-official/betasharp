using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Chunks.Light;

internal struct LightUpdate
{
    public readonly LightType LightType;

    private int _minX;
    private int _minY;
    private int _minZ;
    private int _maxX;
    private int _maxY;
    private int _maxZ;

    public LightUpdate(LightType lightType, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        LightType = lightType;
        _minX = minX;
        _minY = minY;
        _minZ = minZ;
        _maxX = maxX;
        _maxY = maxY;
        _maxZ = maxZ;
    }

    public void UpdateLight(World world)
    {
        int sizeX = _maxX - _minX + 1;
        int sizeY = _maxY - _minY + 1;
        int sizeZ = _maxZ - _minZ + 1;
        int updateVolume = sizeX * sizeY * sizeZ;

        if (updateVolume > -short.MinValue)
        {
            // _logger.LogInformation("Light too large, skipping!");
            return;
        }

        int startY = _minY < 0 ? 0 : _minY;
        int endY = _maxY >= 128 ? 127 : _maxY;

        int cachedChunkX = 0;
        int cachedChunkZ = 0;
        bool isCacheValid = false;
        bool isCachedChunkLoaded = false;

        for (int x = _minX; x <= _maxX; ++x)
        {
            for (int z = _minZ; z <= _maxZ; ++z)
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
                    isChunkLoaded = world.isRegionLoaded(x, 0, z, 1);
                    if (isChunkLoaded)
                    {
                        Chunk chunk = world.GetChunk(chunkX, chunkZ);
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
                        int currentLight = world.getBrightness(LightType, x, y, z);
                        int blockId = world.getBlockId(x, y, z);

                        int opacity = Block.BlockLightOpacity[blockId];
                        if (opacity == 0) opacity = 1;

                        int emittedLight = 0;
                        if (LightType == LightType.Sky)
                        {
                            if (world.isTopY(x, y, z)) emittedLight = 15;
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
                            int l1 = world.getBrightness(LightType, x - 1, y, z);
                            int l2 = world.getBrightness(LightType, x + 1, y, z);
                            int l3 = world.getBrightness(LightType, x, y - 1, z);
                            int l4 = world.getBrightness(LightType, x, y + 1, z);
                            int l5 = world.getBrightness(LightType, x, y, z - 1);
                            int l6 = world.getBrightness(LightType, x, y, z + 1);

                            targetLight = l1;
                            if (l2 > targetLight) targetLight = l2;
                            if (l3 > targetLight) targetLight = l3;
                            if (l4 > targetLight) targetLight = l4;
                            if (l5 > targetLight) targetLight = l5;
                            if (l6 > targetLight) targetLight = l6;

                            targetLight -= opacity;
                            if (targetLight < 0) targetLight = 0;
                            if (emittedLight > targetLight) targetLight = emittedLight;
                        }

                        if (currentLight != targetLight)
                        {
                            world.setLight(LightType, x, y, z, targetLight);

                            int prop = targetLight - 1;
                            if (prop < 0) prop = 0;

                            world.updateLight(LightType, x - 1, y, z, prop);
                            world.updateLight(LightType, x, y - 1, z, prop);
                            world.updateLight(LightType, x, y, z - 1, prop);

                            if (x + 1 >= _maxX) world.updateLight(LightType, x + 1, y, z, prop);
                            if (y + 1 >= _maxY) world.updateLight(LightType, x, y + 1, z, prop);
                            if (z + 1 >= _maxZ) world.updateLight(LightType, x, y, z + 1, prop);
                        }
                    }
                }
            }
        }
    }

    public bool Expand(int reqMinX, int reqMinY, int reqMinZ, int reqMaxX, int reqMaxY, int reqMaxZ)
    {
        if (reqMinX >= _minX && reqMinY >= _minY && reqMinZ >= _minZ &&
            reqMaxX <= _maxX && reqMaxY <= _maxY && reqMaxZ <= _maxZ)
        {
            return true;
        }

        byte expandTolerance = 1;

        if (reqMinX >= _minX - expandTolerance && reqMinY >= _minY - expandTolerance && reqMinZ >= _minZ - expandTolerance &&
            reqMaxX <= _maxX + expandTolerance && reqMaxY <= _maxY + expandTolerance && reqMaxZ <= _maxZ + expandTolerance)
        {
            int oldVolumeX = _maxX - _minX;
            int oldVolumeY = _maxY - _minY;
            int oldVolumeZ = _maxZ - _minZ;

            int newMinX = reqMinX > _minX ? _minX : reqMinX;
            int newMinY = reqMinY > _minY ? _minY : reqMinY;
            int newMinZ = reqMinZ > _minZ ? _minZ : reqMinZ;
            int newMaxX = reqMaxX < _maxX ? _maxX : reqMaxX;
            int newMaxY = reqMaxY < _maxY ? _maxY : reqMaxY;
            int newMaxZ = reqMaxZ < _maxZ ? _maxZ : reqMaxZ;

            int newVolumeX = newMaxX - newMinX;
            int newVolumeY = newMaxY - newMinY;
            int newVolumeZ = newMaxZ - newMinZ;

            int oldVolume = oldVolumeX * oldVolumeY * oldVolumeZ;
            int newVolume = newVolumeX * newVolumeY * newVolumeZ;

            if (newVolume - oldVolume <= 2)
            {
                _minX = newMinX;
                _minY = newMinY;
                _minZ = newMinZ;
                _maxX = newMaxX;
                _maxY = newMaxY;
                _maxZ = newMaxZ;
                return true;
            }
        }

        return false;
    }
}
