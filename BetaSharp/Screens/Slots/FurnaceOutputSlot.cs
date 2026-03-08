using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Screens.Slots;

internal class FurnaceOutputSlot : Slot
{

    private EntityPlayer thePlayer;

    public FurnaceOutputSlot(EntityPlayer var1, IInventory var2, int var3, int var4, int var5) : base(var2, var3, var4, var5)
    {
        thePlayer = var1;
    }

    public override bool canInsert(ItemStack var1)
    {
        return false;
    }

    public override void onTakeItem(ItemStack var1)
    {
        var1.onCraft(thePlayer._ctx, thePlayer);
        if (var1.itemId == Item.IronIngot.id)
        {
            thePlayer.increaseStat(Achievements.AcquireIron, 1);
        }

        if (var1.itemId == Item.CookedFish.id)
        {
            thePlayer.increaseStat(Achievements.CookFish, 1);
        }

        base.onTakeItem(var1);
    }
}
