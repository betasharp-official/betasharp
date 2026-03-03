using BetaSharp.Inventorys;
using BetaSharp.Items;

namespace BetaSharp.Screens.Slots;

public class Slot
{
    private readonly int slotIndex;
    private readonly IInventory inventory;
    public int id;
    public int xDisplayPosition;
    public int yDisplayPosition;

    public Slot(IInventory inv, int index, int x, int y)
    {
        inventory = inv;
        slotIndex = index;
        xDisplayPosition = x;
        yDisplayPosition = y;
    }

    public virtual void onTakeItem(ItemStack stack)
    {
        markDirty();
    }

    public virtual bool canInsert(ItemStack stack)
    {
        return true;
    }

    public ItemStack getStack()
    {
        return inventory.getStack(slotIndex);
    }

    public bool hasStack()
    {
        return getStack() != null;
    }

    public void setStack(ItemStack? var1)
    {
        inventory.setStack(slotIndex, var1);
        markDirty();
    }

    public void markDirty()
    {
        inventory.markDirty();
    }

    public virtual int getMaxItemCount()
    {
        return inventory.getMaxCountPerStack();
    }

    public int getBackgroundTextureId()
    {
        return -1;
    }

    public ItemStack takeStack(int amount)
    {
        return inventory.removeStack(slotIndex, amount);
    }

    public bool Equals(IInventory inventory, int index)
    {
        return inventory == this.inventory && index == slotIndex;
    }
}
