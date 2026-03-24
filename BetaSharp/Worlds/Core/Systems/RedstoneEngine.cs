using BetaSharp.Blocks;

namespace BetaSharp.Worlds.Core.Systems;

public class RedstoneEngine
{
    private readonly IBlockReader _world;

    public RedstoneEngine(IBlockReader world) => _world = world;

    public bool IsStrongPoweringSide(int x, int y, int z, int side)
    {
        int blockId = _world.GetBlockId(x, y, z);
        return blockId != 0 && Block.Blocks[blockId].isStrongPoweringSide(_world, x, y, z, side);
    }

    public bool IsStrongPowered(int x, int y, int z)
    {
        if (IsStrongPoweringSide(x, y - 1, z, 0))
        {
            return true; // Down
        }

        if (IsStrongPoweringSide(x, y + 1, z, 1))
        {
            return true; // Up
        }

        if (IsStrongPoweringSide(x, y, z - 1, 2))
        {
            return true; // North
        }

        if (IsStrongPoweringSide(x, y, z + 1, 3))
        {
            return true; // South
        }

        if (IsStrongPoweringSide(x - 1, y, z, 4))
        {
            return true; // West
        }

        return IsStrongPoweringSide(x + 1, y, z, 5); // East
    }

    public bool IsPoweringSide(int x, int y, int z, int side)
    {
        if (_world.ShouldSuffocate(x, y, z))
        {
            return IsStrongPowered(x, y, z);
        }

        int blockId = _world.GetBlockId(x, y, z);
        return blockId != 0 && Block.Blocks[blockId].isPoweringSide(_world, x, y, z, side);
    }

    public bool IsPowered(int x, int y, int z)
    {
        if (IsPoweringSide(x, y - 1, z, 0))
        {
            return true; // Down
        }

        if (IsPoweringSide(x, y + 1, z, 1))
        {
            return true; // Up
        }

        if (IsPoweringSide(x, y, z - 1, 2))
        {
            return true; // North
        }

        if (IsPoweringSide(x, y, z + 1, 3))
        {
            return true; // South
        }

        if (IsPoweringSide(x - 1, y, z, 4))
        {
            return true; // West
        }

        return IsPoweringSide(x + 1, y, z, 5); // East
    }
}
