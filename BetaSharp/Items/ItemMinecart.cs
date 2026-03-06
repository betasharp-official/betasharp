using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Items;

internal class ItemMinecart : Item
{

    public int minecartType;

    public ItemMinecart(int id, int minecartType) : base(id)
    {
        maxCount = 1;
        this.minecartType = minecartType;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, World world, int x, int y, int z, int meta)
    {
        int blockId = world.getBlockId(x, y, z);
        if (BlockRail.isRail(blockId))
        {
            if (!world.IsRemote)
            {
                world.Entities.SpawnEntity(new EntityMinecart(world, (double)((float)x + 0.5F), (double)((float)y + 0.5F), (double)((float)z + 0.5F), minecartType));
            }

            --itemStack.count;
            return true;
        }
        else
        {
            return false;
        }
    }
}
