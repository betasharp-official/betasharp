using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemSoup : ItemFood
{

    public ItemSoup(int id, int healAmount) : base(id, healAmount, false)
    {
    }

    public override ItemStack use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        base.use(itemStack, world, entityPlayer);
        return new ItemStack(Item.Bowl);
    }
}
