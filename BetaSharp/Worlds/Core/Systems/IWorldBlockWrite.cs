namespace BetaSharp.Worlds.Core.Systems;

public interface IBlockWrite
{
    public bool SetBlock(int x, int y, int z, int blockId);
    public bool SetBlock(int x, int y, int z, int blockId, int meta);
    public void SetBlockMeta(int x, int y, int z, int meta);
    public bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta);
    public event Action<int, int, int, int>? OnBlockChanged;
    public event Action<int, int, int, int>? OnNeighborsShouldUpdate;
    public bool SetBlockInternal(int x, int y, int z, int id, int meta = 0);
}
