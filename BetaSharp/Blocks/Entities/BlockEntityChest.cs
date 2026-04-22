using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;

namespace BetaSharp.Blocks.Entities;

internal class BlockEntityChest : BlockEntity, IInventory
{
    public override BlockEntityType Type => Chest;
    private ItemStack?[] _inventory = new ItemStack[36];

    public int Size => 27;

    public ItemStack? GetStack(int stackIndex)
    {
        return _inventory[stackIndex];
    }

    public ItemStack? RemoveStack(int slot, int amount)
    {
        if (_inventory[slot] != null)
        {
            ItemStack itemStack;
            ItemStack? stack = _inventory[slot];
            if (stack == null) return null;
            if (stack.Count <= amount)
            {
                itemStack = stack;
                _inventory[slot] = null;
                MarkDirty();
                return itemStack;
            }

            itemStack = stack.Split(amount);
            if (stack.Count == 0)
            {
                _inventory[slot] = null;
            }

            MarkDirty();
            return itemStack;
        }

        return null;
    }

    public void SetStack(int slot, ItemStack? stack)
    {
        _inventory[slot] = stack;
        if (stack != null && stack.Count > MaxCountPerStack)
        {
            stack.Count = MaxCountPerStack;
        }

        MarkDirty();
    }

    public string Name => "Chest";

    public int MaxCountPerStack => 64;

    public bool CanPlayerUse(EntityPlayer player)
    {
        return World.Entities.GetBlockEntity<BlockEntityChest>(X, Y, Z) == this && player.GetSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        NBTTagList itemList = nbt.GetTagList("Items");
        _inventory = new ItemStack[Size];

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemsTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            int slot = itemsTag.GetByte("Slot") & 255;
            if (slot >= 0 && slot < _inventory.Length)
            {
                _inventory[slot] = new ItemStack(itemsTag);
            }
        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        NBTTagList itemList = new();

        for (int slotIndex = 0; slotIndex < _inventory.Length; ++slotIndex)
        {
            ItemStack? stack = _inventory[slotIndex];
            if (stack != null)
            {
                NBTTagCompound itemsTag = new();
                itemsTag.SetByte("Slot", (sbyte)slotIndex);
                stack.writeToNBT(itemsTag);
                itemList.SetTag(itemsTag);
            }
        }

        nbt.SetTag("Items", itemList);
    }
}
