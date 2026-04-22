using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Screens;

namespace BetaSharp.Inventorys;

public class InventoryCrafting : IInventory
{
    private readonly ItemStack?[] _stacks;
    private readonly int _width;
    private readonly ScreenHandler _handler;

    public InventoryCrafting(ScreenHandler eventHandler, int width, int height)
    {
        int gridSize = width * height;
        _stacks = new ItemStack[gridSize];
        _handler = eventHandler;
        _width = width;
    }

    public int Size => _stacks.Length;

    public ItemStack? GetStack(int slotIndex)
    {
        return slotIndex >= Size ? null : _stacks[slotIndex];
    }

    public ItemStack? GetStackAt(int x, int y)
    {
        if (x >= 0 && x < _width)
        {
            int slotIndex = x + y * _width;
            return GetStack(slotIndex);
        }
        else
        {
            return null;
        }
    }

    public string Name => "Crafting";

    public ItemStack? RemoveStack(int slotIndex, int amount)
    {
        ItemStack? stack = _stacks[slotIndex];

        if (stack != null)
        {
            ItemStack removeStack;
            if (stack.Count <= amount)
            {
                removeStack = stack;
                _stacks[slotIndex] = null;
                _handler.onSlotUpdate(this);
                return removeStack;
            }
            else
            {
                removeStack = stack.Split(amount);
                if (stack.Count == 0)
                {
                    _stacks[slotIndex] = null;
                }

                _handler.onSlotUpdate(this);
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
        _stacks[slotIndex] = itemStack;
        _handler.onSlotUpdate(this);
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
