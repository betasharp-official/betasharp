using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;

namespace BetaSharp.Blocks.Entities;

/// <summary>
/// Block entity for a chest, storing the inventory.
/// </summary>
public class BlockEntityChest : BlockEntityWithInventory<BlockEntityChest>
{
    public override BlockEntityType Type => Chest;

    public override int Size => 27;

    public override string Name => "Chest";
}