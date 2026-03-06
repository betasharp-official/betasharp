using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockSponge : Block
{
    public BlockSponge(int id) : base(id, Material.Sponge)
    {
        textureId = 48;
    }

    public override void onPlaced(World world, int x, int y, int z)
    {
        sbyte radius = 2;

        for (int checkX = x - radius; checkX <= x + radius; ++checkX)
        {
            for (int checkY = y - radius; checkY <= y + radius; ++checkY)
            {
                for (int checkZ = z - radius; checkZ <= z + radius; ++checkZ)
                {
                    if (world.GetMaterial(checkX, checkY, checkZ) == Material.Water)
                    {
                    }
                }
            }
        }

    }

    public override void onBreak(World world, int x, int y, int z)
    {
        sbyte radius = 2;

        for (int checkX = x - radius; checkX <= x + radius; ++checkX)
        {
            for (int checkY = y - radius; checkY <= y + radius; ++checkY)
            {
                for (int checkZ = z - radius; checkZ <= z + radius; ++checkZ)
                {
                    world.NotifyNeighbors(checkX, checkY, checkZ, world.GetBlockId(checkX, checkY, checkZ));
                }
            }
        }

    }
}
