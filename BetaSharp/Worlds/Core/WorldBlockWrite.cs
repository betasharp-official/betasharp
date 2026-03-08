using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core;

/// <summary>
/// Handles all block write operations (set, meta, dirty notifications).
/// Depends on <see cref="BlockHost"/> for chunk access and block reading.
/// </summary>
public sealed class WorldBlockWrite : IBlockWrite
{
    private readonly BlockHost _host;

    public event Action<int, int, int, int>? OnBlockChanged;
    public event Action<int, int, int, int>? OnNeighborsShouldUpdate;

    public WorldBlockWrite(BlockHost host)
    {
        _host = host;
    }

    // --- IWorldBlockWrite ---

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128) return false;
        return _host.GetChunk(x >> 4, z >> 4).SetBlock(x & 15, y, z & 15, blockId, meta);
    }

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000 && y is >= 0 and < 128)
        {
            return _host.GetChunk(x >> 4, z >> 4).SetBlock(x & 15, y, z & 15, blockId);
        }

        return false;
    }

    public bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000 && y is >= 0 and < 128)
        {
            _host.GetChunk(x >> 4, z >> 4).SetBlockMeta(x & 15, y, z & 15, meta);
            return true;
        }

        return false;
    }

    public bool SetBlock(int x, int y, int z, int blockId)
    {
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId))
        {
            OnBlockChanged?.Invoke(x, y, z, blockId);
            return true;
        }

        return false;
    }

    public bool SetBlock(int x, int y, int z, int blockId, int meta)
    {
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta))
        {
            OnBlockChanged?.Invoke(x, y, z, blockId);
            return true;
        }

        return false;
    }

    public void SetBlockMeta(int x, int y, int z, int meta)
    {
        if (SetBlockMetaWithoutNotifyingNeighbors(x, y, z, meta))
        {
            int blockId = _host.GetBlockId(x, y, z);
            if (Block.BlocksIngoreMetaUpdate[blockId & 255])
            {
                OnBlockChanged?.Invoke(x, y, z, blockId);
            }
            else
            {
                OnNeighborsShouldUpdate?.Invoke(x, y, z, blockId);
            }
        }
    }

    public void SetBlocksDirty(int x, int z, int minY, int maxY)
    {
        if (minY > maxY)
        {
            (maxY, minY) = (minY, maxY);
        }

        SetBlocksDirty(x, minY, z, x, maxY, z);
    }

    public void SetBlocksDirty(int x, int y, int z)
    {
        for (int i = 0; i < _host.EventListeners.Count; ++i)
        {
            _host.EventListeners[i].setBlocksDirty(x, y, z, x, y, z);
        }
    }

    public void SetBlocksDirty(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        for (int i = 0; i < _host.EventListeners.Count; ++i)
        {
            _host.EventListeners[i].setBlocksDirty(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }
}
