using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemMinecart : Item
{

    public int minecartType;

    public ItemMinecart(int id, int minecartType) : base(id)
    {
        maxCount = 1;
        this.minecartType = minecartType;
    }

    public override bool useOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        int blockId = world.Reader.GetBlockId(x, y, z);
        if (BlockRail.isRail(blockId))
        {
            if (!world.IsRemote)
            {
                world.SpawnEntity(new EntityMinecart(world, x + 0.5F, y + 0.5F, z + 0.5F, minecartType));
            }

            itemStack.ConsumeItem(entityPlayer);
            return true;
        }
        else
        {
            return false;
        }
    }
}
