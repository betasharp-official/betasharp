using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemSeeds : Item
{

    private int blockId;

    public ItemSeeds(int id, int blockId) : base(id)
    {
        this.blockId = blockId;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (meta != 1)
        {
            return false;
        }
        else
        {
            int blockId = world.Reader.GetBlockId(x, y, z);
            if (blockId == Block.Farmland.id && world.Reader.IsAir(x, y + 1, z))
            {
                world.Writer.SetBlock(x, y + 1, z, this.blockId);
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
