using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Screens.Slots;

namespace BetaSharp.Screens;

public class GenericContainerScreenHandler : ScreenHandler
{

    private IInventory inventory;
    private int rows;

    public GenericContainerScreenHandler(IInventory playerInventory, IInventory inventory)
    {
        this.inventory = inventory;
        rows = inventory.size() / 9;
        int var3 = (rows - 4) * 18;

        int var4;
        int var5;
        for (var4 = 0; var4 < rows; ++var4)
        {
            for (var5 = 0; var5 < 9; ++var5)
            {
                AddSlot(new Slot(inventory, var5 + var4 * 9, 8 + var5 * 18, 18 + var4 * 18));
            }
        }

        for (var4 = 0; var4 < 3; ++var4)
        {
            for (var5 = 0; var5 < 9; ++var5)
            {
                AddSlot(new Slot(playerInventory, var5 + var4 * 9 + 9, 8 + var5 * 18, 103 + var4 * 18 + var3));
            }
        }

        for (var4 = 0; var4 < 9; ++var4)
        {
            AddSlot(new Slot(playerInventory, var4, 8 + var4 * 18, 161 + var3));
        }

    }

    public override bool canUse(EntityPlayer player)
    {
        return inventory.canPlayerUse(player);
    }

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack var2 = null;
        Slot var3 = Slots[slotNumber];
        if (var3 != null && var3.hasStack())
        {
            ItemStack var4 = var3.getStack();
            var2 = var4.copy();
            if (slotNumber < rows * 9)
            {
                insertItem(var4, rows * 9, Slots.Count, true);
            }
            else
            {
                insertItem(var4, 0, rows * 9, false);
            }

            if (var4.count == 0)
            {
                var3.setStack(null);
            }
            else
            {
                var3.markDirty();
            }
        }

        return var2;
    }
}
