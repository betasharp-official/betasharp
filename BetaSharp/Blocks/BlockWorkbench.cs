using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockWorkbench : Block
{

    public BlockWorkbench(int id) : base(id, Material.Wood)
    {
        textureId = 59;
    }

    public override int getTexture(int side)
    {
        return side == 1 ? textureId - 16 : (side == 0 ? Block.Planks.getTexture(0) : (side != 2 && side != 4 ? textureId : textureId + 1));
    }

    public override bool onUse(World world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsRemote)
        {
            return true;
        }
        else
        {
            player.openCraftingScreen(x, y, z);
            return true;
        }
    }
}
