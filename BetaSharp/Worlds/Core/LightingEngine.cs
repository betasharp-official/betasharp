using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Dimensions;
using BetaSharp.Worlds.Lighting;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Core;

public class LightingEngine
{
    private readonly World _world;
    private readonly ILogger<LightingEngine> _logger = Log.Instance.For<LightingEngine>();

    private readonly List<LightUpdate> _lightingQueue = [];
    private int _lightingUpdatesCounter;
    private int _lightingUpdatesScheduled;

    // TODO: Replace 'World' dependency with specific scoped interfaces/deps to prevent circular dependencies.
    public LightingEngine(World world)
    {
        _world = world;
    }

    // TODO: Many places lioke ness need to use the BlockPos struc
    public float GetNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int lightLevel = GetLightLevel(x, y, z);
        if (lightLevel < blockLight) lightLevel = blockLight;
        return _world.Dimension.LightLevelToLuminance[lightLevel];
    }

    public float GetLuminance(int x, int y, int z) => _world.Dimension.LightLevelToLuminance[GetLightLevel(x, y, z)];

    public bool HasSkyLight(int x, int y, int z) => _world.GetChunk(x >> 4, z >> 4).IsAboveMaxHeight(x & 15, y, z & 15);

    public int GetBrightness(int x, int y, int z)
    {
        if (y < 0) return 0;
        if (y >= 128) return !_world.Dimension.HasCeiling ? 15 : 0;
        return _world.GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, 0);
    }

    public int GetLightLevel(int x, int y, int z) => GetLightLevel(x, y, z, true);

    public int GetLightLevel(int x, int y, int z, bool checkNeighbors)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return 15;

        if (checkNeighbors)
        {
            int blockId = _world.GetBlockId(x, y, z);
            if (blockId == Block.Slab.id || blockId == Block.Farmland.id ||
                blockId == Block.CobblestoneStairs.id || blockId == Block.WoodenStairs.id)
            {
                int neighborMaxLight = GetLightLevel(x, y + 1, z, false);
                int lightPosX = GetLightLevel(x + 1, y, z, false);
                int lightNegX = GetLightLevel(x - 1, y, z, false);
                int lightPosZ = GetLightLevel(x, y, z + 1, false);
                int lightNegZ = GetLightLevel(x, y, z - 1, false);

                if (lightPosX > neighborMaxLight) neighborMaxLight = lightPosX;
                if (lightNegX > neighborMaxLight) neighborMaxLight = lightNegX;
                if (lightPosZ > neighborMaxLight) neighborMaxLight = lightPosZ;
                if (lightNegZ > neighborMaxLight) neighborMaxLight = lightNegZ;

                return neighborMaxLight;
            }
        }

        if (y < 0) return 0;
        if (y >= 128) return !_world.Dimension.HasCeiling ? 15 - _world.ambientDarkness : 0;

        Chunk chunk = _world.GetChunk(x >> 4, z >> 4);
        return chunk.GetLight(x & 15, y, z & 15, _world.ambientDarkness);
    }

    public void UpdateLight(LightType lightType, int x, int y, int z, int targetLuminance)
    {
        if (_world.Dimension.HasCeiling && lightType == LightType.Sky) return;

        if (_world.IsPosLoaded(x, y, z))
        {
            if (lightType == LightType.Sky)
            {
                if (_world.IsTopY(x, y, z)) targetLuminance = 15;
            }
            else if (lightType == LightType.Block)
            {
                int blockId = _world.GetBlockId(x, y, z);
                if (Block.BlocksLightLuminance[blockId] > targetLuminance)
                {
                    targetLuminance = Block.BlocksLightLuminance[blockId];
                }
            }

            if (GetBrightness(lightType, x, y, z) != targetLuminance)
            {
                QueueLightUpdate(lightType, x, y, z, x, y, z);
            }
        }
    }

    public int GetBrightness(LightType type, int x, int y, int z)
    {
        if (y < 0) y = 0;
        if (y >= 128) return type.lightValue;

        if (y >= 0 && y < 128 && x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            int chunkX = x >> 4;
            int chunkZ = z >> 4;
            if (!_world.HasChunk(chunkX, chunkZ)) return 0;

            Chunk chunk = _world.GetChunk(chunkX, chunkZ);
            return chunk.GetLight(type, x & 15, y, z & 15);
        }

        return type.lightValue;
    }

    public void SetLight(LightType lightType, int x, int y, int z, int value)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y >= 0 && y < 128)
            {
                if (_world.HasChunk(x >> 4, z >> 4))
                {
                    Chunk chunk = _world.GetChunk(x >> 4, z >> 4);
                    chunk.SetLight(lightType, x & 15, y, z & 15, value);
                    _world.BlockUpdateEvent(x, y, z);
                }
            }
        }
    }

    public bool DoLightingUpdates()
    {
        if (_lightingUpdatesCounter >= 50) return false;

        ++_lightingUpdatesCounter;
        try
        {
            int updatesBudget = 500;

            while (_lightingQueue.Count > 0)
            {
                if (updatesBudget <= 0) return true;
                updatesBudget--;

                int lastIndex = _lightingQueue.Count - 1;
                LightUpdate updateTask = _lightingQueue[lastIndex];

                _lightingQueue.RemoveAt(lastIndex);
                updateTask.updateLight(_world);
            }

            return false;
        }
        finally
        {
            --_lightingUpdatesCounter;
        }
    }

    public void QueueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        => QueueLightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ, true);

    public void QueueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, bool attemptMerge)
    {
        if (_world.Dimension.HasCeiling && type == LightType.Sky) return;

        ++_lightingUpdatesScheduled;
        try
        {
            if (_lightingUpdatesScheduled == 50) return;

            int centerX = (maxX + minX) / 2;
            int centerZ = (maxZ + minZ) / 2;

            if (_world.IsPosLoaded(centerX, 64, centerZ))
            {
                if (_world.GetChunkFromPos(centerX, centerZ).IsEmpty()) return;

                int queueSize = _lightingQueue.Count;
                Span<LightUpdate> span = CollectionsMarshal.AsSpan(_lightingQueue);

                if (attemptMerge)
                {
                    int lookbackCount = Math.Min(5, queueSize);
                    for (int i = 0; i < lookbackCount; ++i)
                    {
                        ref LightUpdate existingUpdate = ref span[queueSize - i - 1];
                        if (existingUpdate.lightType == type &&
                            existingUpdate.expand(minX, minY, minZ, maxX, maxY, maxZ))
                        {
                            return;
                        }
                    }
                }

                _lightingQueue.Add(new LightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ));

                const int maxQueueCapacity = 1000000;
                if (_lightingQueue.Count > maxQueueCapacity)
                {
                    _logger.LogInformation($"More than {maxQueueCapacity} updates, aborting lighting updates");
                    _lightingQueue.Clear();
                }
            }
        }
        finally
        {
            --_lightingUpdatesScheduled;
        }
    }
}
