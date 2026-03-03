using BetaSharp.Entities;
using BetaSharp.Items;
using java.util;

namespace BetaSharp.Inventorys;

public class InventoryBasic : IInventory
{

    private string inventoryTitle;
    private int slotsCount;
    private ItemStack[] inventoryContents;
    private List field_20073_d;

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

    public ItemStack removeStack(int slotIndex, int amount)
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
        if (field_20073_d != null)
        {
            for (int slotIndex = 0; slotIndex < field_20073_d.size(); ++slotIndex)
            {
                ((IInvBasic)field_20073_d.get(slotIndex)).func_20134_a(this);
            }
        }

    }

    public bool canPlayerUse(EntityPlayer entityPlayer)
    {
        return true;
    }
}
