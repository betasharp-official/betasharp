using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemSeeds : Item
{

    private int blockId;

    public ItemSeeds(int id, int blockId) : base(id)
    {
        this.blockId = blockId;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        if (meta != 1)
        {
            return false;
        }
        else
        {
            int blockId = world.GetBlockId(x, y, z);
            if (blockId == Block.Farmland.id && world.IsAir(x, y + 1, z))
            {
                world.SetBlock(x, y + 1, z, this.blockId);
                --itemStack.count;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
