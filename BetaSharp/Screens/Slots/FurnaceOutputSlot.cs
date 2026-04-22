using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Screens.Slots;

internal class FurnaceOutputSlot : Slot
{

    private EntityPlayer thePlayer;

    public FurnaceOutputSlot(EntityPlayer player, IInventory inventory, int slotIndex, int x, int y) : base(inventory, slotIndex, x, y)
    {
        thePlayer = player;
    }

    public override bool canInsert(ItemStack stack)
    {
        return false;
    }

    public override void onTakeItem(ItemStack stack)
    {
        stack.onCraft(thePlayer.World, thePlayer);
        if (stack.ItemId == Item.IronIngot.id)
        {
            thePlayer.increaseStat(Achievements.AcquireIron, 1);
        }

        if (stack.ItemId == Item.CookedFish.id)
        {
            thePlayer.increaseStat(Achievements.CookFish, 1);
        }

        base.onTakeItem(stack);
    }
}
