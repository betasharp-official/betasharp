using System.Runtime.InteropServices;
using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Lighting;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Core.Systems;

public class LightingEngine : ILightProvider
{
    private readonly IWorldContext _world;

    private readonly List<LightUpdate> _lightingQueue = [];
    private readonly ILogger<LightingEngine> _logger = Log.Instance.For<LightingEngine>();
    private int _lightingUpdatesCounter;
    private int _lightingUpdatesScheduled;

    public LightingEngine(IWorldContext world)
    {
        _world = world;
    }

    public float GetNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int lightLevel = GetLightLevel(x, y, z);
        if (lightLevel < blockLight)
        {
            lightLevel = blockLight;
        }

        return _world.Dimension.LightLevelToLuminance[lightLevel];
    }

    public float GetLuminance(int x, int y, int z) => _world.Dimension.LightLevelToLuminance[GetLightLevel(x, y, z)];

    public event Action<int, int, int>? OnLightUpdated;

    public bool HasSkyLight(int x, int y, int z) => _world.ChunkHost.GetChunk(x >> 4, z >> 4).IsAboveMaxHeight(x & 15, y, z & 15);

    public int GetBrightness(int x, int y, int z)
    {
        return y switch
        {
            < 0 => 0,
            >= 128 => !_world.Dimension.HasCeiling ? 15 : 0,
            _ => _world.ChunkHost.GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, 0)
        };
    }

    public int GetLightLevel(int x, int y, int z) => GetLightLevel(x, y, z, true);

    public int GetLightLevel(int x, int y, int z, bool checkNeighbors)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000)
        {
            return 15;
        }

        if (checkNeighbors)
        {
            int blockId = _world.Reader.GetBlockId(x, y, z);
            if (blockId == Block.Slab.id || blockId == Block.Farmland.id ||
                blockId == Block.CobblestoneStairs.id || blockId == Block.WoodenStairs.id)
            {
                int neighborMaxLight = GetLightLevel(x, y + 1, z, false);
                int lightPosX = GetLightLevel(x + 1, y, z, false);
                int lightNegX = GetLightLevel(x - 1, y, z, false);
                int lightPosZ = GetLightLevel(x, y, z + 1, false);
                int lightNegZ = GetLightLevel(x, y, z - 1, false);

                if (lightPosX > neighborMaxLight)
                {
                    neighborMaxLight = lightPosX;
                }

                if (lightNegX > neighborMaxLight)
                {
                    neighborMaxLight = lightNegX;
                }

                if (lightPosZ > neighborMaxLight)
                {
                    neighborMaxLight = lightPosZ;
                }

                if (lightNegZ > neighborMaxLight)
                {
                    neighborMaxLight = lightNegZ;
                }

                return neighborMaxLight;
            }
        }

        switch (y)
        {
            case < 0:
                return 0;
            case >= 128:
                return !_world.Dimension.HasCeiling ? 15 - _world.Reader.AmbientDarkness : 0;
            default:
                {
                    Chunk chunk = _world.ChunkHost.GetChunk(x >> 4, z >> 4);
                    return chunk.GetLight(x & 15, y, z & 15, _world.Reader.AmbientDarkness);
                }
        }
    }

    public void UpdateLight(LightType lightType, int x, int y, int z, int targetLuminance)
    {
        if (_world.Dimension.HasCeiling && lightType == LightType.Sky)
        {
            return;
        }

        if (_world.Reader.IsPosLoaded(x, y, z))
        {
            if (lightType == LightType.Sky)
            {
                if (_world.Reader.IsTopY(x, y, z))
                {
                    targetLuminance = 15;
                }
            }
            else if (lightType == LightType.Block)
            {
                int blockId = _world.Reader.GetBlockId(x, y, z);
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
        if (y < 0)
        {
            y = 0;
        }

        switch (y)
        {
            case >= 128:
                return type.lightValue;
            case >= 0 and < 128 when x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000:
                {
                    int chunkX = x >> 4;
                    int chunkZ = z >> 4;
                    if (!_world.ChunkHost.HasChunk(chunkX, chunkZ))
                    {
                        return 0;
                    }

                    Chunk chunk = _world.ChunkHost.GetChunk(chunkX, chunkZ);
                    return chunk.GetLight(type, x & 15, y, z & 15);
                }
            default:
                return type.lightValue;
        }
    }

    public void SetLight(LightType lightType, int x, int y, int z, int value)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000)
        {
            if (y >= 0 && y < 128)
            {
                if (_world.ChunkHost.HasChunk(x >> 4, z >> 4))
                {
                    Chunk chunk = _world.ChunkHost.GetChunk(x >> 4, z >> 4);
                    chunk.SetLight(lightType, x & 15, y, z & 15, value);
                    OnLightUpdated?.Invoke(x, y, z);
                }
            }
        }
    }

    public bool DoLightingUpdates()
    {
        if (_lightingUpdatesCounter >= 50)
        {
            return false;
        }

        ++_lightingUpdatesCounter;
        try
        {
            int updatesBudget = 500;

            while (_lightingQueue.Count > 0)
            {
                if (updatesBudget <= 0)
                {
                    return true;
                }

                updatesBudget--;

                int lastIndex = _lightingQueue.Count - 1;
                LightUpdate updateTask = _lightingQueue[lastIndex];

                _lightingQueue.RemoveAt(lastIndex);
                updateTask.UpdateLight(_world.Reader, _world.ChunkHost, this);
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
        if (_world.Dimension.HasCeiling && type == LightType.Sky)
        {
            return;
        }

        ++_lightingUpdatesScheduled;
        try
        {
            if (_lightingUpdatesScheduled == 50)
            {
                return;
            }

            int centerX = (maxX + minX) / 2;
            int centerZ = (maxZ + minZ) / 2;

            if (_world.Reader.IsPosLoaded(centerX, 64, centerZ))
            {
                if (_world.ChunkHost.GetChunkFromPos(centerX, centerZ).IsEmpty())
                {
                    return;
                }

                int queueSize = _lightingQueue.Count;
                Span<LightUpdate> span = CollectionsMarshal.AsSpan(_lightingQueue);

                if (attemptMerge)
                {
                    int lookbackCount = Math.Min(5, queueSize);
                    for (int i = 0; i < lookbackCount; ++i)
                    {
                        ref LightUpdate existingUpdate = ref span[queueSize - i - 1];
                        if (existingUpdate.LightType == type &&
                            existingUpdate.Expand(minX, minY, minZ, maxX, maxY, maxZ))
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
