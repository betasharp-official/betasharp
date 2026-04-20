using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;

namespace BetaSharp.Blocks.Entities;

/// <summary>
/// Block entity for a chest, storing the 9 dispener slots and implementing the random item selection.
/// </summary>
public class BlockEntityDispenser : BlockEntityWithInventory<BlockEntityDispenser>
{
    public override BlockEntityType Type => Dispenser;
    public override int Size => 9; // 3x3
    public override string Name => "Trap";

    private readonly JavaRandom _random = new();

    /// <summary>
    /// Chooses a random item from the dispenser inventory and removes one item from it, returning the removed item. Returns null if the inventory is empty.
    /// </summary>
    public ItemStack? GetItemToDispose()
    {
        int selectedSlot = -1;
        int nonNullCount = 1;

        for (int slotIndex = 0; slotIndex < _inventory.Length; ++slotIndex)
        {
            if (_inventory[slotIndex] != null && _random.NextInt(nonNullCount++) == 0)
            {
                selectedSlot = slotIndex;
            }
        }

        if (selectedSlot >= 0)
        {
            return RemoveStack(selectedSlot, 1);
        }

        return null;
    }
}