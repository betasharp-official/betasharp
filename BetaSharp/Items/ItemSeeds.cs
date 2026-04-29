using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemSeeds(int id, int blockId) : Item(id)
{
    public override bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        if (meta != 1)
        {
            return false;
        }

        int blockId1 = world.Reader.GetBlockId(x, y, z);
        if (blockId1 != Block.Farmland.id || !world.Reader.IsAir(x, y + 1, z))
        {
            return false;
        }

        world.Writer.SetBlock(x, y + 1, z, blockId);
        itemStack.ConsumeItem(entityPlayer);
        return true;

    }
}
