using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Recipes;
using BetaSharp.Screens.Slots;

namespace BetaSharp.Screens;

public class PlayerScreenHandler : ScreenHandler
{

    public InventoryCrafting craftingInput;
    public IInventory craftingResult;
    public bool isLocal;

    public PlayerScreenHandler(InventoryPlayer inventoryPlayer) : this(inventoryPlayer, true)
    {
    }

    public PlayerScreenHandler(InventoryPlayer inventoryPlayer, bool isLocal)
    {
        craftingInput = new InventoryCrafting(this, 2, 2);
        craftingResult = new InventoryCraftResult();
        this.isLocal = false;
        this.isLocal = isLocal;
        AddSlot(new CraftingResultSlot(inventoryPlayer.player, craftingInput, craftingResult, 0, 144, 36));

        int var3;
        int var4;
        for (var3 = 0; var3 < 2; ++var3)
        {
            for (var4 = 0; var4 < 2; ++var4)
            {
                AddSlot(new Slot(craftingInput, var4 + var3 * 2, 88 + var4 * 18, 26 + var3 * 18));
            }
        }

        for (var3 = 0; var3 < 4; ++var3)
        {
            AddSlot(new SlotArmor(this, inventoryPlayer, inventoryPlayer.size() - 1 - var3, 8, 8 + var3 * 18, var3));
        }

        for (var3 = 0; var3 < 3; ++var3)
        {
            for (var4 = 0; var4 < 9; ++var4)
            {
                AddSlot(new Slot(inventoryPlayer, var4 + (var3 + 1) * 9, 8 + var4 * 18, 84 + var3 * 18));
            }
        }

        for (var3 = 0; var3 < 9; ++var3)
        {
            AddSlot(new Slot(inventoryPlayer, var3, 8 + var3 * 18, 142));
        }

        onSlotUpdate(craftingInput);
    }

    public override void onSlotUpdate(IInventory inv)
    {
        craftingResult.setStack(0, CraftingManager.getInstance().FindMatchingRecipe(craftingInput));
    }

    public override void onClosed(EntityPlayer player)
    {
        base.onClosed(player);

        for (int var2 = 0; var2 < 4; ++var2)
        {
            ItemStack var3 = craftingInput.getStack(var2);
            if (var3 != null)
            {
                player.dropItem(var3);
                craftingInput.setStack(var2, null);
            }
        }

    }

    public override bool canUse(EntityPlayer player)
    {
        return true;
    }

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack var2 = null;
        Slot var3 = Slots[slotNumber];
        if (var3 != null && var3.hasStack())
        {
            ItemStack var4 = var3.getStack();
            var2 = var4.copy();
            if (slotNumber == 0)
            {
                insertItem(var4, 9, 45, true);
            }
            else if (slotNumber >= 9 && slotNumber < 36)
            {
                insertItem(var4, 36, 45, false);
            }
            else if (slotNumber >= 36 && slotNumber < 45)
            {
                insertItem(var4, 9, 36, false);
            }
            else
            {
                insertItem(var4, 9, 45, false);
            }

            if (var4.count == 0)
            {
                var3.setStack(null);
            }
            else
            {
                var3.markDirty();
            }

            if (var4.count == var2.count)
            {
                return null;
            }

            var3.onTakeItem(var4);
        }

        return var2;
    }
}
