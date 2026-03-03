using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Screens;

namespace BetaSharp.Inventorys;

public class InventoryCrafting : IInventory
{
    private ItemStack[] stackList;
    private int gridWidth;
    private ScreenHandler eventHandler;

    public InventoryCrafting(ScreenHandler eventHandler, int gridWidth, int gridHeight)
    {
        int gridSize = gridWidth * gridHeight;
        stackList = new ItemStack[gridSize];
        this.eventHandler = eventHandler;
        this.gridWidth = gridWidth;
    }

    public int size()
    {
        return stackList.Length;
    }

    public ItemStack getStack(int slotIndex)
    {
        return slotIndex >= size() ? null : stackList[slotIndex];
    }

    public ItemStack getStackAt(int x, int y)
    {
        if (x >= 0 && x < gridWidth)
        {
            int slotIndex = x + y * gridWidth;
            return getStack(slotIndex);
        }
        else
        {
            return null;
        }
    }

    public string getName()
    {
        return "Crafting";
    }

    public ItemStack removeStack(int slotIndex, int amount)
    {
        if (stackList[slotIndex] != null)
        {
            ItemStack removeStack;
            if (stackList[slotIndex].count <= amount)
            {
                removeStack = stackList[slotIndex];
                stackList[slotIndex] = null;
                eventHandler.onSlotUpdate(this);
                return removeStack;
            }
            else
            {
                removeStack = stackList[slotIndex].split(amount);
                if (stackList[slotIndex].count == 0)
                {
                    stackList[slotIndex] = null;
                }

                eventHandler.onSlotUpdate(this);
                return removeStack;
            }
        }
        else
        {
            return null;
        }
    }

    public void setStack(int slotIndex, ItemStack? itemStack)
    {
        stackList[slotIndex] = itemStack;
        eventHandler.onSlotUpdate(this);
    }

    public int getMaxCountPerStack()
    {
        return 64;
    }

    public void markDirty()
    {
    }

    public bool canPlayerUse(EntityPlayer entityPlayer)
    {
        return true;
    }
}
