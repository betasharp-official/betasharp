namespace BetaSharp.Worlds.Core.Systems;

public interface IBlockWriter
{
    public event Action<int, int, int, int, int, int, int>? OnBlockChangedWithPrev;
    public event Action<int, int, int, int>? OnBlockChanged;
    public event Action<int, int, int, int>? OnNeighborsShouldUpdate;
    public bool SetBlock(int x, int y, int z, int blockId);
    public bool SetBlock(int x, int y, int z, int blockId, int meta);
    public bool SetBlock(int x, int y, int z, int blockId, int meta, bool doUpdate);
    public void SetBlockMeta(int x, int y, int z, int meta);
    public bool SetBlockWithoutCallingOnPlaced(int x, int y, int z, int blockId, int meta);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta, bool notifyBlockPlaced);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId);
    public bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, bool notifyBlockPlaced);
    public bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta);
    public bool SetBlockInternal(int x, int y, int z, int id, int meta = 0);
}
