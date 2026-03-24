using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

internal class InventoryCraftResult : IInventory
{

    private ItemStack[] stackResult = new ItemStack[1];

    public int size()
    {
        return 1;
    }

    public ItemStack getStack(int slotIndex)
    {
        return stackResult[slotIndex];
    }

    public string getName()
    {
        return "Result";
    }

    public ItemStack? removeStack(int slotIndex, int amount)
    {
        if (stackResult[slotIndex] != null)
        {
            ItemStack removeStack = stackResult[slotIndex];
            stackResult[slotIndex] = null;
            return removeStack;
        }
        else
        {
            return null;
        }
    }

    public void setStack(int slotIndex, ItemStack? itemStack)
    {
        stackResult[slotIndex] = itemStack;
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
