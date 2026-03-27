using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityDispenser : BlockEntity, IInventory
{
    public override BlockEntityType Type => BlockEntity.Dispenser;
    private ItemStack?[] _itemStacks = new ItemStack[9];
    private readonly JavaRandom _random = new();

    public int size()
    {
        return 9;
    }

    public ItemStack? getStack(int slot)
    {
        return _itemStacks[slot];
    }

    public ItemStack? removeStack(int slot, int amount)
    {
        var item = _itemStacks[slot];
        if (item != null)
        {
            ItemStack removedStack;
            if (item.count <= amount)
            {
                removedStack = item;
                _itemStacks[slot] = null;
                markDirty();
                return removedStack;
            }

            removedStack = item.split(amount);
            if (item.count == 0)
            {
                _itemStacks[slot] = null;
            }

            markDirty();
            return removedStack;
        }

        return null;
    }

    public void setStack(int slot, ItemStack? stack)
    {
        _itemStacks[slot] = stack;
        if (stack != null && stack.count > getMaxCountPerStack())
        {
            stack.count = getMaxCountPerStack();
        }

        markDirty();
    }

    public string getName()
    {
        return "Trap";
    }

    public int getMaxCountPerStack()
    {
        return 64;
    }

    public bool canPlayerUse(EntityPlayer player)
    {
        return World.Entities.GetBlockEntity<BlockEntityDispenser>(X, Y, Z) == this && player.getSquaredDistance(X + 0.5D, Y + 0.5D, Z + 0.5D) <= 64.0D;
    }

    public ItemStack? getItemToDispose()
    {
        int selectedSlot = -1;
        int nonNullCount = 1;

        for (int slotIndex = 0; slotIndex < _itemStacks.Length; ++slotIndex)
        {
            if (_itemStacks[slotIndex] != null && _random.NextInt(nonNullCount++) == 0)
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
        _itemStacks = new ItemStack[size()];

        for (int itemIndex = 0; itemIndex < itemList.TagCount(); ++itemIndex)
        {
            NBTTagCompound itemTag = (NBTTagCompound)itemList.TagAt(itemIndex);
            int slotIndex = itemTag.GetByte("Slot") & 255;
            if (slotIndex >= 0 && slotIndex < _itemStacks.Length)
            {
                _itemStacks[slotIndex] = new ItemStack(itemTag);
            }
        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        NBTTagList itemList = new();


        for (int slotIndex = 0; slotIndex < _itemStacks.Length; ++slotIndex)
        {
            var itemStack = _itemStacks[slotIndex];
            if (itemStack == null) continue;

            NBTTagCompound itemTag = new();
            itemTag.SetByte("Slot", (sbyte)slotIndex);
            itemStack.writeToNBT(itemTag);
            itemList.SetTag(itemTag);
        }

        nbt.SetTag("Items", itemList);
    }
}
