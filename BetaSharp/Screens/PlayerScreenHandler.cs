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

        int row;
        int column;
        for (row = 0; row < 2; ++row)
        {
            for (column = 0; column < 2; ++column)
            {
                AddSlot(new Slot(craftingInput, column + row * 2, 88 + column * 18, 26 + row * 18));
            }
        }

        for (int armorSlot = 0; armorSlot < 4; ++armorSlot)
        {
            AddSlot(new SlotArmor(this, inventoryPlayer, inventoryPlayer.size() - 1 - armorSlot, 8, 8 + armorSlot * 18, armorSlot));
        }

        for (row = 0; row < 3; ++row)
        {
            for (column = 0; column < 9; ++column)
            {
                AddSlot(new Slot(inventoryPlayer, column + (row + 1) * 9, 8 + column * 18, 84 + row * 18));
            }
        }

        for (int hotbarSlot = 0; hotbarSlot < 9; ++hotbarSlot)
        {
            AddSlot(new Slot(inventoryPlayer, hotbarSlot, 8 + hotbarSlot * 18, 142));
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

        for (int slotIndex = 0; slotIndex < 4; ++slotIndex)
        {
            ItemStack craftingStack = craftingInput.getStack(slotIndex);
            if (craftingStack != null)
            {
                player.inventory.AddItemStackToInventory(craftingStack);
                craftingInput.setStack(slotIndex, null);
            }
        }

    }

    public override bool canUse(EntityPlayer player)
    {
        return true;
    }

    public override ItemStack quickMove(int slotNumber)
    {
        ItemStack movedStack = null;
        Slot slot = Slots[slotNumber];
        if (slot != null && slot.hasStack())
        {
            ItemStack slotStack = slot.getStack();
            movedStack = slotStack.copy();
            if (slotNumber == 0)
            {
                insertItem(slotStack, 9, 45, true);
            }
            else if (slotNumber >= 5 && slotNumber < 9)
            {
                insertItem(slotStack, 9, 45, false);
            }
            else if (slotNumber >= 9 && slotNumber < 45)
            {
                if (slotStack.getItem() is ItemArmor armor)
                {
                    int targetSlot = 5 + armor.armorType;
                    int countBefore = slotStack.count;
                    insertItem(slotStack, targetSlot, targetSlot + 1, false);
                    if (slotStack.count == countBefore)
                    {
                        if (slotNumber < 36)
                        {
                            insertItem(slotStack, 36, 45, false);
                        }
                        else
                        {
                            insertItem(slotStack, 9, 36, false);
                        }
                    }
                }
                else if (slotNumber < 36)
                {
                    insertItem(slotStack, 36, 45, false);
                }
                else
                {
                    insertItem(slotStack, 9, 36, false);
                }
            }
            else
            {
                insertItem(slotStack, 9, 45, false);
            }

            if (slotStack.count == 0)
            {
                slot.setStack(null);
            }
            else
            {
                slot.markDirty();
            }

            if (slotStack.count == movedStack.count)
            {
                return null;
            }

            slot.onTakeItem(slotStack);
        }

        return movedStack;
    }
}
