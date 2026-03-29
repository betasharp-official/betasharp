using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockOre(int id, int textureId) : Block(id, textureId, Material.Stone)
{
    public override int GetDroppedItemId(int blockMeta) => Id == CoalOre.Id ? Item.Coal.id : Id == DiamondOre.Id ? Item.Diamond.id : Id == LapisOre.Id ? Item.Dye.id : Id;

    public override int DroppedItemCount => Id == LapisOre.Id ? 4 + Random.Shared.Next(5) : 1;

    protected override int GetDroppedItemMeta(int blockMeta) => Id == LapisOre.Id ? 4 : 0;
}
