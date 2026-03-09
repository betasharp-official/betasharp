namespace BetaSharp.Worlds.Core;

public interface IBlockWrite
{
   //bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId, int meta);
   //bool SetBlockWithoutNotifyingNeighbors(int x, int y, int z, int blockId);
   //bool SetBlockMetaWithoutNotifyingNeighbors(int x, int y, int z, int meta);
    bool SetBlock(int x, int y, int z, int blockId);
    bool SetBlock(int x, int y, int z, int blockId, int meta);
    void SetBlockMeta(int x, int y, int z, int meta);
    event Action<int, int, int, int>? OnBlockChanged;
    event Action<int, int, int, int>? OnNeighborsShouldUpdate;
}
