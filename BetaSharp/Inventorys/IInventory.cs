using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

public interface IInventory
{
    int Size { get; }

    ItemStack? GetStack(int slotIndex);

    ItemStack? RemoveStack(int slotIndex, int amount);

    void SetStack(int slotIndex, ItemStack? itemStack);

    string Name { get; }

    int MaxCountPerStack { get; }

    void MarkDirty();

    bool CanPlayerUse(EntityPlayer entityPlayer);
}
