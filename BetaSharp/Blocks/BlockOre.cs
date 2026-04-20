using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockOre(int id, int textureId) : Block(id, textureId, Material.Stone)
{
    public override int GetDroppedItemId(int blockMeta) => ID == CoalOre.ID ? Item.Coal.id : ID == DiamondOre.ID ? Item.Diamond.id : ID == LapisOre.ID ? Item.Dye.id : ID;

    public override int GetDroppedItemCount() => ID == LapisOre.ID ? 4 + Random.Shared.Next(5) : 1;

    protected override int GetDroppedItemMeta(int blockMeta) => ID == LapisOre.ID ? 4 : 0;
}
