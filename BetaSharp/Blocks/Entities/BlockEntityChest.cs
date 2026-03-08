using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;

namespace BetaSharp.Blocks.Entities;

internal class BlockEntityChest : BlockEntity, IInventory
{
    private ItemStack[] inventory = new ItemStack[36];

    public int size() => 27;

    public ItemStack getStack(int stackIndex) => inventory[stackIndex];

    public ItemStack removeStack(int slot, int amount)
    {
        if (inventory[slot] != null)
        {
            ItemStack itemStack;
            if (inventory[slot].count <= amount)
            {
                itemStack = inventory[slot];
                inventory[slot] = null;
                markDirty();
                return itemStack;
            }

            itemStack = inventory[slot].split(amount);
            if (inventory[slot].count == 0)
            {
                inventory[slot] = null;
            }

            markDirty();
            return itemStack;
        }

        return null;
    }

    public void setStack(int slot, ItemStack? stack)
    {
        inventory[slot] = stack;
        if (stack != null && stack.count > getMaxCountPerStack())
        {
            stack.count = getMaxCountPerStack();
        }

        markDirty();
    }

    public string getName() => "Chest";

    public int getMaxCountPerStack() => 64;

    public bool canPlayerUse(EntityPlayer player) => World.getBlockEntity(X, Y, Z) != this ? false : player.getSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        NBTTagList itemList = nbt.GetTagList("Items");
        inventory = new ItemStack[size()];

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemsTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            int slot = itemsTag.GetByte("Slot") & 255;
            if (slot >= 0 && slot < inventory.Length)
            {
                inventory[slot] = new ItemStack(itemsTag);
            }
        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        NBTTagList itemList = new();

        for (int slotIndex = 0; slotIndex < inventory.Length; ++slotIndex)
        {
            if (inventory[slotIndex] != null)
            {
                NBTTagCompound itemsTag = new();
                itemsTag.SetByte("Slot", (sbyte)slotIndex);
                inventory[slotIndex].writeToNBT(itemsTag);
                itemList.SetTag(itemsTag);
            }
        }

        nbt.SetTag("Items", itemList);
    }
}
