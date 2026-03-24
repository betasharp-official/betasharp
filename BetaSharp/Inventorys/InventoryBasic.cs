using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

public class InventoryBasic : IInventory
{

    private string inventoryTitle;
    private int slotsCount;
    private ItemStack[] inventoryContents;

    public InventoryBasic(string inventoryTitle, int slotsCount)
    {
        this.inventoryTitle = inventoryTitle;
        this.slotsCount = slotsCount;
        inventoryContents = new ItemStack[slotsCount];
    }

    public ItemStack getStack(int slotIndex)
    {
        return inventoryContents[slotIndex];
    }

    public ItemStack? removeStack(int slotIndex, int amount)
    {
        if (inventoryContents[slotIndex] != null)
        {
            ItemStack removeStack;
            if (inventoryContents[slotIndex].count <= amount)
            {
                removeStack = inventoryContents[slotIndex];
                inventoryContents[slotIndex] = null;
                markDirty();
                return removeStack;
            }
            else
            {
                removeStack = inventoryContents[slotIndex].split(amount);
                if (inventoryContents[slotIndex].count == 0)
                {
                    inventoryContents[slotIndex] = null;
                }

                markDirty();
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
        inventoryContents[slotIndex] = itemStack;
        if (itemStack != null && itemStack.count > getMaxCountPerStack())
        {
            itemStack.count = getMaxCountPerStack();
        }

        markDirty();
    }

    public int size()
    {
        return slotsCount;
    }

    public string getName()
    {
        return inventoryTitle;
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
