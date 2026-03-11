using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core.Systems;

/// <summary>
///     Handles all block write operations (set, meta, dirty notifications).
///     Depends on <see cref="ChunkHost" /> for chunk access and block reading.
/// </summary>
public sealed class WorldBlockWrite : IBlockWrite
{
    private readonly ChunkHost _host;
    private readonly IBlockReader _reader;

    public WorldBlockWrite(ChunkHost host, IBlockReader reader)
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
        return SetBlock(x, y, z, blockId, meta, doUpdate: true);
    }

    public bool SetBlock(int x, int y, int z, int blockId, int meta, bool doUpdate)
    {
        int prevId = _reader.GetBlockId(x, y, z);
        int prevMeta = _reader.GetMeta(x, y, z);
        if (SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta, notifyBlockPlaced: doUpdate))
        {
            if (doUpdate)
            {
                OnBlockChanged?.Invoke(x, y, z, blockId);
                OnBlockChangedWithPrev?.Invoke(x, y, z, prevId, prevMeta, blockId, meta);
            }
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
        return SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, meta, notifyBlockPlaced: true);
    }

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta, bool notifyBlockPlaced)
    {
        if (x < -32000000 || z < -32000000 || x >= 32000000 || z > 32000000 || y < 0 || y >= 128)
        {
            return false;
        }

        return _host.GetChunk(x >> 4, z >> 4).SetBlock(x & 15, y, z & 15, blockId, meta, notifyBlockPlaced);
    }

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId)
    {
        return SetBlockWithoutNotifyingNeighbors(x, y, z, blockId, notifyBlockPlaced: true);
    }

    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, bool notifyBlockPlaced)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000 && y is >= 0 and < 128)
        {
            return _host.GetChunk(x >> 4, z >> 4).SetBlock(x & 15, y, z & 15, blockId, notifyBlockPlaced);
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
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000 && y is >= 0 and < 128)
        {
            return _host.GetChunk(x >> 4, z >> 4).SetBlock(x & 15, y, z & 15, id, meta);
        }

        return false;
    }

    public bool SetBlockMetaInternal(int x, int y, int z, int meta)
    {
        if (x >= -32000000 && z >= -32000000 && x < 32000000 && z <= 32000000 && y is >= 0 and < 128)
        {
            _host.GetChunk(x >> 4, z >> 4).SetBlockMeta(x & 15, y, z & 15, meta);
            return true;
        }

        return false;
    }
}
