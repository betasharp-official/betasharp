using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

internal class InventoryCraftResult : IInventory
{

    private readonly ItemStack?[] _result = new ItemStack[1];

    public int Size => 1;

    public ItemStack? GetStack(int slotIndex)
    {
        return _result[slotIndex];
    }

    public string Name => "Result";

    public ItemStack? RemoveStack(int slotIndex, int amount)
    {
        ItemStack? stack = _result[slotIndex];

        if (stack != null)
        {
            ItemStack removeStack = stack;
            _result[slotIndex] = null;
            return removeStack;
        }
        else
        {
            return null;
        }
    }

    public void SetStack(int slotIndex, ItemStack? itemStack)
    {
        _result[slotIndex] = itemStack;
    }

    public int MaxCountPerStack => 64;

    public void MarkDirty()
    {
    }

    public bool CanPlayerUse(EntityPlayer entityPlayer)
    {
        return true;
    }
}
