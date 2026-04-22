using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

public class InventoryBasic(string inventoryTitle, int slotsCount) : IInventory
{
    private readonly ItemStack?[] _inventoryContents = new ItemStack[slotsCount];

    public ItemStack? GetStack(int slotIndex)
    {
        return _inventoryContents[slotIndex];
    }

    public ItemStack? RemoveStack(int slotIndex, int amount)
    {
        ItemStack? inSlot = _inventoryContents[slotIndex];

        if (inSlot != null)
        {
            ItemStack removeStack;
            if (inSlot.Count <= amount)
            {
                removeStack = inSlot;
                _inventoryContents[slotIndex] = null;
                MarkDirty();
                return removeStack;
            }
            else
            {
                removeStack = inSlot.Split(amount);
                if (inSlot.Count == 0)
                {
                    _inventoryContents[slotIndex] = null;
                }

                MarkDirty();
                return removeStack;
            }
        }
        else
        {
            return null;
        }
    }

    public void SetStack(int slotIndex, ItemStack? itemStack)
    {
        _inventoryContents[slotIndex] = itemStack;
        if (itemStack != null && itemStack.Count > MaxCountPerStack)
        {
            itemStack.Count = MaxCountPerStack;
        }

        MarkDirty();
    }

    public int Size { get; } = slotsCount;

    public string Name { get; } = inventoryTitle;

    public int MaxCountPerStack => 64;

    public void MarkDirty()
    {
    }

    public bool CanPlayerUse(EntityPlayer entityPlayer)
    {
        return true;
    }
}
