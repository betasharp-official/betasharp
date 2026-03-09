using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core.Systems;

/// <summary>
///     Handles all block write operations (set, meta, dirty notifications).
///     Depends on <see cref="BlockHost" /> for chunk access and block reading.
/// </summary>
public sealed class WorldBlockWrite : IBlockWrite
{
    private readonly BlockHost _host;
    private readonly IBlockReader _reader;

    public WorldBlockWrite(BlockHost host, IBlockReader reader)
    {
        _host = host;
        _reader = reader;
    }

    public event Action<int, int, int, int>? OnBlockChanged;
    public event Action<int, int, int, int>? OnNeighborsShouldUpdate;

    /// <summary>
    /// Fires after a block write, carrying (x, y, z, prevId, prevMeta, newId, newMeta).
    /// </summary>
    public event Action<int, int, int, int, int, int, int>? OnBlockChangedWithPrev;

    public bool SetBlock(int x, int y, int z, int blockId)
    {
        int prevId = _reader.GetBlockId(x, y, z);
        int prevMeta = _reader.GetMeta(x, y, z);
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId))
        {
            OnBlockChanged?.Invoke(x, y, z, blockId);
            OnBlockChangedWithPrev?.Invoke(x, y, z, prevId, prevMeta, blockId, 0);
            return true;
        }

        return false;
    }

    public bool SetBlock(int x, int y, int z, int blockId, int meta)
    {
        int prevId = _reader.GetBlockId(x, y, z);
        int prevMeta = _reader.GetMeta(x, y, z);
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta))
        {
            OnBlockChanged?.Invoke(x, y, z, blockId);
            OnBlockChangedWithPrev?.Invoke(x, y, z, prevId, prevMeta, blockId, meta);
            return true;
        }

        return false;
    }

    public void SetBlockMeta(int x, int y, int z, int meta)
    {
        if (SetBlockMetaWithoutNotifyingNeighbors(x, y, z, meta))
        {
            int blockId = _reader.GetBlockId(x, y, z);
            if (Block.BlocksIgnoreMetaUpdate[blockId & 255])
            {
                OnBlockChanged?.Invoke(x, y, z, blockId);
            }
            else
            {
                OnNeighborsShouldUpdate?.Invoke(x, y, z, blockId);
            }
        }
    }

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128)
        {
            return false;
        }

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

    public bool SetBlockInternal(int x, int y, int z, int id, int meta = 0)
    {
        return _host.GetChunk(x, z).SetBlock(x, y, z, id, meta);
    }

    public bool SetBlockMetaInternal(int x, int y, int z, int meta)
    {
        return _host.GetChunk(x, z).SetBlockMeta(x, y, z, meta);
    }
}
