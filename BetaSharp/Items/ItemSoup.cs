using BetaSharp.Entities;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Items;

internal class ItemSoup(int id, int healAmount) : ItemFood(id, healAmount, false)
{
    public override ItemStack Use(ItemStack itemStack, IWorldContext world, EntityPlayer entityPlayer)
    {
        base.Use(itemStack, world, entityPlayer);
        return new ItemStack(Bowl);
    }
}
