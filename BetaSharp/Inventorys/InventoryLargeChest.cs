using BetaSharp.Blocks.Entities;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

internal class InventoryLargeChest : BlockEntity, IInventory
{
    private string name;
    private IInventory upperChest;
    private IInventory lowerChest;

    public InventoryLargeChest(string name, IInventory upperChest, IInventory lowerChest)
    {
        this.name = name;
        this.upperChest = upperChest;
        this.lowerChest = lowerChest;
    }

    public int size()
    {
        return upperChest.size() + lowerChest.size();
    }

    public string getName()
    {
        return name;
    }

    public ItemStack getStack(int slotIndex)
    {
        return slotIndex >= upperChest.size() ? lowerChest.getStack(slotIndex - upperChest.size()) : upperChest.getStack(slotIndex);
    }

    public ItemStack removeStack(int slotIndex, int amount)
    {
        return slotIndex >= upperChest.size() ? lowerChest.removeStack(slotIndex - upperChest.size(), amount) : upperChest.removeStack(slotIndex, amount);
    }

    public void setStack(int slotIndex, ItemStack? itemStack)
    {
        if (slotIndex >= upperChest.size())
        {
            lowerChest.setStack(slotIndex - upperChest.size(), itemStack);
        }
        else
        {
            upperChest.setStack(slotIndex, itemStack);
        }

    }

    public int getMaxCountPerStack()
    {
        return upperChest.getMaxCountPerStack();
    }

    public void markDirty()
    {
        upperChest.markDirty();
        lowerChest.markDirty();
    }

    public bool canPlayerUse(EntityPlayer entityPlayer)
    {
        return upperChest.canPlayerUse(entityPlayer) && lowerChest.canPlayerUse(entityPlayer);
    }
}
