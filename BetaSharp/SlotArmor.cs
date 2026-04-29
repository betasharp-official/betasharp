using BetaSharp.Blocks;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens;
using BetaSharp.Screens.Slots;

namespace BetaSharp;

class SlotArmor : Slot
{
    readonly int armorType;
    readonly PlayerScreenHandler inventory;

    public SlotArmor(PlayerScreenHandler screenHandler, IInventory inventory, int slotIndex, int x, int y, int armorType) : base(inventory, slotIndex, x, y)
    {
        this.inventory = screenHandler;
        this.armorType = armorType;
    }


    public override int getMaxItemCount()
    {
        return 1;
    }

    public override bool canInsert(ItemStack stack)
    {
        return stack.GetItem() is ItemArmor
            ? ((ItemArmor)stack.GetItem()).ArmorType == armorType
            : stack.GetItem().Id == Block.Pumpkin.id && armorType == 0;
    }
}
