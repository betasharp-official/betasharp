using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

public interface IInventory
{
    int size();

    ItemStack getStack(int slotIndex);

    ItemStack? removeStack(int slotIndex, int amount);

    void setStack(int slotIndex, ItemStack? itemStack);

    string getName();

    int getMaxCountPerStack();

    void markDirty();

    bool canPlayerUse(EntityPlayer entityPlayer);
}
