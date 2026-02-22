using BetaSharp.Entities;
using BetaSharp.Worlds;

namespace BetaSharp.Items;

public class ItemTotem : Item
{
    public ItemTotem(int id) : base(id)
    {
        maxCount = 1;
    }
    public override ItemStack use(ItemStack itemStack, World world, EntityPlayer entityPlayer)
    {
        return itemStack;
    }

    public ItemStack saveLife(ItemStack itemStack, World world, EntityPlayer entityPlayer)
    {
        --itemStack.count;
        itemStack.count = itemStack.count < 0 ? 0 : itemStack.count;
        //This is very stupid code
        entityPlayer.inventory.main[entityPlayer.inventory.selectedSlot] = null;
        entityPlayer.heal(1000);
        return itemStack;
    }
}