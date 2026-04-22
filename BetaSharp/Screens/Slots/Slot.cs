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

    public ItemStack? getStack()
    {
        return inventory.GetStack(slotIndex);
    }

    public bool hasStack()
    {
        return getStack() != null;
    }

    public void setStack(ItemStack? stack)
    {
        inventory.SetStack(slotIndex, stack);
        markDirty();
    }

    public void markDirty()
    {
        inventory.MarkDirty();
    }

    public virtual int getMaxItemCount()
    {
        return inventory.MaxCountPerStack;
    }

    public static int getBackgroundTextureId()
    {
        return -1;
    }

    public ItemStack? takeStack(int amount)
    {
        return inventory.RemoveStack(slotIndex, amount);
    }

    public bool Equals(IInventory inventory, int index)
    {
        return inventory == this.inventory && index == slotIndex;
    }
}
