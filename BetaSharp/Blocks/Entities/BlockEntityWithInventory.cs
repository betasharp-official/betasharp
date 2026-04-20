using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;

namespace BetaSharp.Blocks.Entities;

/// <summary>
///     Abstract class for block entities with an inventory.
/// </summary>
/// <typeparam name="T">The type of the block entity with inventory, just for CanPlayerUse method.</typeparam>
public abstract class BlockEntityWithInventory<T> : BlockEntity, IInventory where T : BlockEntityWithInventory<T>
{
    public BlockEntityWithInventory() => _inventory = new ItemStack?[Size];

    protected ItemStack?[] _inventory { get; }
    public abstract int Size { get; } // heirs must implement this to specify the inventory size
    public abstract string Name { get; } // heirs must implement this to specify the inventory name

    public int MaxCountPerStack => 64; // default max count per stack, can be overridden by heirs if needed

    public ItemStack? GetStack(int stackIndex) => _inventory[stackIndex];

    public virtual ItemStack? RemoveStack(int slot, int amount)
    {
        ItemStack? item = _inventory[slot];
        if (item != null)
        {
            ItemStack removedStack;
            if (item.Count <= amount)
            {
                removedStack = item;
                _inventory[slot] = null;
                MarkDirty();
                return removedStack;
            }

            removedStack = item.Split(amount);
            if (item.Count == 0)
            {
                _inventory[slot] = null;
            }

            MarkDirty();
            return removedStack;
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

    public bool CanPlayerUse(EntityPlayer player) => World.Entities.GetBlockEntity<T>(X, Y, Z) == this && player.GetSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        NBTTagList itemList = nbt.GetTagList("Items");

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            int slotIndex = itemTag.GetByte("Slot") & 255;
            if (slotIndex >= 0 && slotIndex < _inventory.Length)
            {
                _inventory[slotIndex] = new ItemStack(itemTag);
            }
            else
            {
                _inventory[slotIndex] = null;
            }
        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        NBTTagList itemList = new();

        for (int slotIndex = 0; slotIndex < _inventory.Length; ++slotIndex)
        {
            ItemStack? itemStack = _inventory[slotIndex];
            if (itemStack == null)
            {
                continue;
            }

            NBTTagCompound itemTag = new();
            itemTag.SetByte("Slot", (sbyte)slotIndex);
            itemStack.writeToNBT(itemTag);
            itemList.SetTag(itemTag);
        }

        nbt.SetTag("Items", itemList);
    }
}
