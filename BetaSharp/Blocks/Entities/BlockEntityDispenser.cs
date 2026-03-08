using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityDispenser : BlockEntity, IInventory
{
    private readonly JavaRandom random = new();
    private ItemStack[] inventory = new ItemStack[9];

    public int size() => 9;

    public ItemStack getStack(int slot) => inventory[slot];

    public ItemStack removeStack(int slot, int amount)
    {
        if (inventory[slot] != null)
        {
            ItemStack removedStack;
            if (inventory[slot].count <= amount)
            {
                removedStack = inventory[slot];
                inventory[slot] = null;
                markDirty();
                return removedStack;
            }

            removedStack = inventory[slot].split(amount);
            if (inventory[slot].count == 0)
            {
                inventory[slot] = null;
            }

            markDirty();
            return removedStack;
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

    public string getName() => "Trap";

    public int getMaxCountPerStack() => 64;

    public bool canPlayerUse(EntityPlayer player) => World.getBlockEntity(X, Y, Z) != this ? false : player.getSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;

    public ItemStack getItemToDispose()
    {
        int selectedSlot = -1;
        int nonNullCount = 1;

        for (int slotIndex = 0; slotIndex < inventory.Length; ++slotIndex)
        {
            if (inventory[slotIndex] != null && random.NextInt(nonNullCount++) == 0)
            {
                selectedSlot = slotIndex;
            }
        }

        if (selectedSlot >= 0)
        {
            return removeStack(selectedSlot, 1);
        }

        return null;
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        NBTTagList itemList = nbt.GetTagList("Items");
        inventory = new ItemStack[size()];

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            int slotIndex = itemTag.GetByte("Slot") & 255;
            if (slotIndex >= 0 && slotIndex < inventory.Length)
            {
                inventory[slotIndex] = new ItemStack(itemTag);
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
                NBTTagCompound itemTag = new();
                itemTag.SetByte("Slot", (sbyte)slotIndex);
                inventory[slotIndex].writeToNBT(itemTag);
                itemList.SetTag(itemTag);
            }
        }

        nbt.SetTag("Items", itemList);
    }
}
