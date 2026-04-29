using BetaSharp.Blocks;
using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemMinecart : Item
{
    private readonly int _minecartType;

    public ItemMinecart(int id, int minecartType) : base(id)
    {
        MaxCount = 1;
        _minecartType = minecartType;
    }

    public override bool UseOnBlock(ItemStack itemStack, EntityPlayer entityPlayer, IWorldContext world, int x, int y, int z, int meta)
    {
        int blockId = world.Reader.GetBlockId(x, y, z);
        if (!BlockRail.isRail(blockId))
        {
            return false;
        }

        if (!world.IsRemote)
        {
            world.SpawnEntity(new EntityMinecart(world, x + 0.5F, y + 0.5F, z + 0.5F, _minecartType));
        }

        itemStack.ConsumeItem(entityPlayer);
        return true;

    }
}
