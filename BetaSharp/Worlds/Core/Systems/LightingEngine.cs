using System.Collections.Concurrent;
using BetaSharp.Blocks;
using BetaSharp.Worlds.Chunks;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Core.Systems;

public class LightingEngine : ILightProvider
{
    private readonly IWorldContext _world;
    private readonly ILogger<LightingEngine> _logger = Log.Instance.For<LightingEngine>();

    private readonly struct VolumeUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        public readonly LightType Type = type;
        public readonly int MinX = minX, MinY = minY, MinZ = minZ, MaxX = maxX, MaxY = maxY, MaxZ = maxZ;
    }

    private readonly struct LightRemovalNode(LightType type, int x, int y, int z, int val)
    {
        public readonly LightType Type = type;
        public readonly int X = x, Y = y, Z = z, Val = val;
    }

    private readonly ConcurrentQueue<VolumeUpdate> _volumeQueue = new();
    private readonly ConcurrentQueue<long> _blockQueue = new();
    private readonly ConcurrentQueue<LightRemovalNode> _lightRemovalQueue = new();
    private readonly ConcurrentDictionary<long, byte> _blockSet = new();

    private readonly ConcurrentDictionary<long, byte> _dirtyChunksSync = new();

    public bool IsIdle => _volumeQueue.IsEmpty && _blockQueue.IsEmpty && _lightRemovalQueue.IsEmpty && _dirtyChunksSync.IsEmpty;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public LightingEngine(IWorldContext world)
    {
        _world = world;
        Task.Factory.StartNew(LightingLoop, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public event Action<int, int, int>? OnLightUpdated;

    private static long Pack(LightType type, int x, int y, int z)
    {
        long bType = type == LightType.Sky ? 1L : 0L;
        long bY = y & 0x7FL;
        long bX = x & 0xFFFFFFFL;
        long bZ = z & 0xFFFFFFFL;
        return (bType << 63) | (bY << 56) | (bX << 28) | bZ;
    }

    private static void Unpack(long packed, out LightType type, out int x, out int y, out int z)
    {
        long bType = (packed >> 63) & 1L;
        type = bType == 1L ? LightType.Sky : LightType.Block;
        y = (int)((packed >> 56) & 0x7FL);
        x = ((int)((packed >> 28) & 0xFFFFFFFL) << 4) >> 4;
        z = ((int)(packed & 0xFFFFFFFL) << 4) >> 4;
    }

    public float GetNaturalBrightness(int x, int y, int z, int blockLight)
    {
        int lightLevel = GetLightLevel(x, y, z);
        if (lightLevel < blockLight) lightLevel = blockLight;
        return _world.Dimension.LightLevelToLuminance[lightLevel];
    }

    public float GetLuminance(int x, int y, int z) => _world.Dimension.LightLevelToLuminance[GetLightLevel(x, y, z)];

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
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return 15;

        if (checkNeighbors)
        {
            int blockId = _world.Reader.GetBlockId(x, y, z);
            if (blockId == Block.Slab.id || blockId == Block.Farmland.id ||
                blockId == Block.CobblestoneStairs.id || blockId == Block.WoodenStairs.id)
            {
                int max = GetLightLevel(x, y + 1, z, false);
                max = Math.Max(max, GetLightLevel(x + 1, y, z, false));
                max = Math.Max(max, GetLightLevel(x - 1, y, z, false));
                max = Math.Max(max, GetLightLevel(x, y, z + 1, false));
                max = Math.Max(max, GetLightLevel(x, y, z - 1, false));
                return max;
            }
        }

        return y switch
        {
            < 0 => 0,
            >= 128 => !_world.Dimension.HasCeiling ? 15 - _world.Environment.AmbientDarkness : 0,
            _ => _world.ChunkHost.GetChunk(x >> 4, z >> 4).GetLight(x & 15, y, z & 15, _world.Environment.AmbientDarkness)
        };
    }

    public int GetBrightness(LightType type, int x, int y, int z)
    {
        if (y < 0) y = 0;
        if (y >= 128 || x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000) return type.LightValue;

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        return !_world.ChunkHost.HasChunk(chunkX, chunkZ) ? 0 : _world.ChunkHost.GetChunk(chunkX, chunkZ).GetLight(type, x & 15, y, z & 15);
    }

    public void SetLight(LightType lightType, int x, int y, int z, int value)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y is < 0 or >= 128) return;

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        if (!_world.ChunkHost.HasChunk(chunkX, chunkZ)) return;

        Chunk chunk = _world.ChunkHost.GetChunk(chunkX, chunkZ);
        chunk.SetLight(lightType, x & 15, y, z & 15, value);
        chunk.MarkDirty();

        long chunkPacked = ((long)chunkX << 32) | (uint)chunkZ;
        _dirtyChunksSync.TryAdd(chunkPacked, 0);
    }

    public void UpdateLight(LightType lightType, int x, int y, int z, int targetLuminance)
    {
        if (_world.Dimension.HasCeiling && lightType == LightType.Sky) return;
        if (!_world.Reader.IsPosLoaded(x, y, z)) return;

        int oldLight = GetBrightness(lightType, x, y, z);

        if (oldLight > 0)
        {
            SetLight(lightType, x, y, z, 0);
            _lightRemovalQueue.Enqueue(new LightRemovalNode(lightType, x, y, z, oldLight));
        }

        QueueNode(lightType, x, y, z);
        QueueNode(lightType, x - 1, y, z);
        QueueNode(lightType, x + 1, y, z);
        QueueNode(lightType, x, y - 1, z);
        QueueNode(lightType, x, y + 1, z);
        QueueNode(lightType, x, y, z - 1);
        QueueNode(lightType, x, y, z + 1);
    }

    public void QueueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        => QueueLightUpdate(type, minX, minY, minZ, maxX, maxY, maxZ, true);

    public void QueueLightUpdate(LightType type, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, bool attemptMerge)
    {
        if (_world.Dimension.HasCeiling && type == LightType.Sky) return;

        if (minX == maxX && minY == maxY && minZ == maxZ)
        {
            QueueNode(type, minX, minY, minZ);
            QueueNode(type, minX - 1, minY, minZ);
            QueueNode(type, minX + 1, minY, minZ);
            QueueNode(type, minX, minY - 1, minZ);
            QueueNode(type, minX, minY + 1, minZ);
            QueueNode(type, minX, minY, minZ - 1);
            QueueNode(type, minX, minY, minZ + 1);
            return;
        }

        _volumeQueue.Enqueue(new VolumeUpdate(type, minX, minY, minZ, maxX, maxY, maxZ));
    }

    private void QueueNode(LightType type, int x, int y, int z)
    {
        if (y is < 0 or > 127) return;

        long packed = Pack(type, x, y, z);

        if (_blockSet.TryAdd(packed, 0))
        {
            _blockQueue.Enqueue(packed);
        }
    }

    public bool DoLightingUpdates(int networkBudget = 15)
    {
        bool didWork = false;

        foreach (long packed in _dirtyChunksSync.Keys)
        {
            if (networkBudget <= 0) break;

            if (!_dirtyChunksSync.TryRemove(packed, out _)) continue;

            int cx = (int)(packed >> 32);
            int cz = (int)packed;
            OnLightUpdated?.Invoke(cx * 16 + 8, 64, cz * 16 + 8);

            didWork = true;

            networkBudget--;
        }

        return didWork;
    }

    private void LightingLoop()
    {
        _logger.LogInformation("Background Lighting Thread initialized.");

        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                bool didWork = false;

                if (_volumeQueue.TryDequeue(out VolumeUpdate vol))
                {
                    ProcessVolumeUpdate(vol);
                    didWork = true;
                }

                int batchSize = _blockQueue.Count > 5000 || _lightRemovalQueue.Count > 5000 ? 50_000 : 10_000;

                while (batchSize > 0 && _lightRemovalQueue.TryDequeue(out LightRemovalNode node))
                {
                    batchSize--;
                    ProcessLightRemoval(node);
                    didWork = true;
                }

                if (_lightRemovalQueue.IsEmpty)
                {
                    while (batchSize > 0 && _blockQueue.TryDequeue(out long packed))
                    {
                        batchSize--;
                        _blockSet.TryRemove(packed, out _);

                        Unpack(packed, out LightType type, out int x, out int y, out int z);
                        ProcessSingleLightNode(type, x, y, z);
                        didWork = true;
                    }
                }

                if (!didWork)
                {
                    Thread.Sleep(10);
                }
                else if (_blockQueue.Count < 1000)
                {
                    Thread.Sleep(1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FATAL: Background Lighting Thread crashed!");
            _volumeQueue.Clear();
            _blockQueue.Clear();
            _dirtyChunksSync.Clear();
        }
    }

    private void ProcessVolumeUpdate(VolumeUpdate vol)
    {
        int startY = Math.Max(0, vol.MinY);
        int endY = Math.Min(127, vol.MaxY);

        for (int x = vol.MinX; x <= vol.MaxX; ++x)
        {
            for (int z = vol.MinZ; z <= vol.MaxZ; ++z)
            {
                if (!_world.ChunkHost.HasChunk(x >> 4, z >> 4)) continue;

                Chunk chunk = _world.ChunkHost.GetChunk(x >> 4, z >> 4);
                if (chunk.IsEmpty()) continue;

                for (int y = startY; y <= endY; ++y)
                {
                    ProcessSingleLightNode(vol.Type, x, y, z);
                }
            }
        }
    }

    public void InitializeChunkInterior(int chunkX, int chunkZ)
    {
        int startX = chunkX * 16;
        int startZ = chunkZ * 16;
        int endX = startX + 15;
        int endZ = startZ + 15;
        QueueLightUpdate(LightType.Sky, startX, 0, startZ, endX, 127, endZ);
        QueueLightUpdate(LightType.Block, startX, 0, startZ, endX, 127, endZ);
    }

    private void ProcessSingleLightNode(LightType type, int x, int y, int z)
    {
        if (!_world.ChunkHost.IsRegionLoaded(x, 0, z, 1)) return;

        Chunk chunk = _world.ChunkHost.GetChunk(x >> 4, z >> 4);
        if (chunk.IsEmpty()) return;

        int currentLight = GetBrightness(type, x, y, z);
        int blockId = _world.Reader.GetBlockId(x, y, z);

        int opacity = Block.BlockLightOpacity[blockId];

        int emittedLight = 0;
        if (type == LightType.Sky && _world.Reader.IsTopY(x, y, z) && opacity < 15)
        {
            emittedLight = 15;
        }
        else if (type == LightType.Block)
        {
            emittedLight = Block.BlocksLightLuminance[blockId];
        }

        int targetLight = 0;
        if (opacity >= 15 && emittedLight == 0)
        {
            targetLight = 0;
        }
        else
        {
            int attenuation = opacity == 0 ? 1 : opacity;

            int l1 = GetBrightness(type, x - 1, y, z);
            int l2 = GetBrightness(type, x + 1, y, z);
            int l3 = GetBrightness(type, x, y - 1, z);
            int l4 = GetBrightness(type, x, y + 1, z);
            int l5 = GetBrightness(type, x, y, z - 1);
            int l6 = GetBrightness(type, x, y, z + 1);

            targetLight = l1;
            if (l2 > targetLight) targetLight = l2;
            if (l3 > targetLight) targetLight = l3;
            if (l5 > targetLight) targetLight = l5;
            if (l6 > targetLight) targetLight = l6;

            targetLight -= attenuation;

            int downwardLight = l4;
            if (type == LightType.Sky && l4 == 15 && opacity == 0)
            {
                downwardLight = 15;
            }
            else
            {
                downwardLight -= attenuation;
            }

            if (downwardLight > targetLight) targetLight = downwardLight;

            if (targetLight < 0) targetLight = 0;
            if (emittedLight > targetLight) targetLight = emittedLight;
        }

        if (currentLight == targetLight) return;

        SetLight(type, x, y, z, targetLight);

        QueueNode(type, x - 1, y, z);
        QueueNode(type, x + 1, y, z);
        QueueNode(type, x, y - 1, z);
        QueueNode(type, x, y + 1, z);
        QueueNode(type, x, y, z - 1);
        QueueNode(type, x, y, z + 1);
    }

    public void StitchChunkBorders(int chunkX, int chunkZ)
    {
        int startX = chunkX * 16;
        int startZ = chunkZ * 16;
        int endX = startX + 15;
        int endZ = startZ + 15;

        QueueLightUpdate(LightType.Sky, startX - 1, 0, startZ - 1, endX + 1, 127, startZ);
        QueueLightUpdate(LightType.Block, startX - 1, 0, startZ - 1, endX + 1, 127, startZ);
        QueueLightUpdate(LightType.Sky, startX - 1, 0, endZ, endX + 1, 127, endZ + 1);
        QueueLightUpdate(LightType.Block, startX - 1, 0, endZ, endX + 1, 127, endZ + 1);
        QueueLightUpdate(LightType.Sky, startX - 1, 0, startZ, startX, 127, endZ);
        QueueLightUpdate(LightType.Block, startX - 1, 0, startZ, startX, 127, endZ);
        QueueLightUpdate(LightType.Sky, endX, 0, startZ, endX + 1, 127, endZ);
        QueueLightUpdate(LightType.Block, endX, 0, startZ, endX + 1, 127, endZ);
    }

    private void ProcessLightRemoval(LightRemovalNode node)
    {
        CheckRemovalNeighbor(node.Type, node.X - 1, node.Y, node.Z, node.Val, false, node.X, node.Y, node.Z);
        CheckRemovalNeighbor(node.Type, node.X + 1, node.Y, node.Z, node.Val, false, node.X, node.Y, node.Z);
        CheckRemovalNeighbor(node.Type, node.X, node.Y - 1, node.Z, node.Val, true, node.X, node.Y, node.Z);
        CheckRemovalNeighbor(node.Type, node.X, node.Y + 1, node.Z, node.Val, false, node.X, node.Y, node.Z);
        CheckRemovalNeighbor(node.Type, node.X, node.Y, node.Z - 1, node.Val, false, node.X, node.Y, node.Z);
        CheckRemovalNeighbor(node.Type, node.X, node.Y, node.Z + 1, node.Val, false, node.X, node.Y, node.Z);
    }

    private void CheckRemovalNeighbor(LightType type, int x, int y, int z, int oldLight, bool isDownward, int voidX, int voidY, int voidZ)
    {
        if (y is < 0 or > 127) return;
        if (!_world.ChunkHost.IsRegionLoaded(x, 0, z, 1)) return;

        int currentLight = GetBrightness(type, x, y, z);
        if (currentLight == 0) return;

        bool isSkyBeam = isDownward && type == LightType.Sky && currentLight == oldLight;

        if (currentLight < oldLight || isSkyBeam)
        {
            SetLight(type, x, y, z, 0);
            _lightRemovalQueue.Enqueue(new LightRemovalNode(type, x, y, z, currentLight));
        }
        else if (currentLight >= oldLight)
        {
            QueueNode(type, voidX, voidY, voidZ);
        }
    }

    public void Shutdown()
    {
        _cancellationTokenSource.Cancel();
    }
}
