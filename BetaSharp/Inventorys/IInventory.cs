using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Inventorys;

public interface IInventory
{
    /// <summary>
    /// Size (amount of ItemStacks) the inventory can hold.
    /// For any function requiring a slot index, the index must be between 0 and <see cref="Size"/>-1, inclusive.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Get a stack from the block entity.
    /// </summary>
    /// <param name="slotIndex">Index (0 to <see cref="Size"/>-1) of the item to get.</param>
    /// <returns>The <see cref="ItemStack"/> at the specified index, or null if empty.</returns>
    ItemStack? GetStack(int slotIndex);

    /// <summary>
    /// Remove "amount" items from the stack at "slotIndex" and return them.
    /// </summary>
    /// <param name="slotIndex">Slot index (0 to <see cref="Size"/>-1) to remove.</param>
    /// <param name="amount">Amount (MUST BE LESS OR EQUAL TO <see cref="MaxCountPerStack"/></param>
    /// <returns></returns>
    ItemStack? RemoveStack(int slotIndex, int amount);

    /// <summary>
    /// Set a stack in the inventory.
    /// </summary>
    /// <param name="slotIndex">The index (0 to <see cref="Size"/>-1) of the inventory slot to set.</param>
    /// <param name="itemStack">The <see cref="ItemStack"/> to set in the slot, or null to clear the slot.</param>
    void SetStack(int slotIndex, ItemStack? itemStack);

    /// <summary>
    /// Name of the inventory, should be overridden!
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Max amount of items per stack.
    /// </summary>
    int MaxCountPerStack { get; }

    /// <summary>
    /// Mark this inventory as dirty, meaning it has been modified and should be sent to the server or such.
    /// </summary>
    void MarkDirty();

    /// <summary>
    /// Returns if a <see cref="EntityPlayer"/> can use this inventory.
    /// Should be overridden to check if the player is close enough or such.
    /// </summary>
    /// <param name="entityPlayer"><see cref="EntityPlayer"/> to check for.</param>
    /// <returns>True if the player can use this inventory.</returns>
    bool CanPlayerUse(EntityPlayer entityPlayer);
}
