using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core.Systems;

public class RedstoneEngine(IBlockReader world)
{
    public bool IsStrongPoweringSide(int x, int y, int z, int side)
    {
        int blockId = world.GetBlockId(x, y, z);
        return blockId != 0 && Block.Blocks[blockId].isStrongPoweringSide(world, x, y, z, side);
    }

    public bool IsStrongPowered(int x, int y, int z)
    {
        return IsStrongPoweringSide(x, y - 1, z, 0) || // Down
               IsStrongPoweringSide(x, y + 1, z, 1) || // Up
               IsStrongPoweringSide(x, y, z - 1, 2) || // North
               IsStrongPoweringSide(x, y, z + 1, 3) || // South
               IsStrongPoweringSide(x - 1, y, z, 4) || // West
               IsStrongPoweringSide(x + 1, y, z, 5); // East
    }

    public bool IsPoweringSide(int x, int y, int z, int side)
    {
        if (world.ShouldSuffocate(x, y, z)) return IsStrongPowered(x, y, z);
        int blockId = world.GetBlockId(x, y, z);
        return blockId != 0 && Block.Blocks[blockId].isPoweringSide(world, x, y, z, side);
    }

    public bool IsPowered(int x, int y, int z)
    {
        return IsPoweringSide(x, y - 1, z, 0) || // Down
               IsPoweringSide(x, y + 1, z, 1) || // Up
               IsPoweringSide(x, y, z - 1, 2) || // North
               IsPoweringSide(x, y, z + 1, 3) || // South
               IsPoweringSide(x - 1, y, z, 4) || // West
               IsPoweringSide(x + 1, y, z, 5); // East
    }
}
