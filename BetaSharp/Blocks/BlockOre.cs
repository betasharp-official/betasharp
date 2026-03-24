using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockOre : Block
{
    public BlockOre(int id, int textureId) : base(id, textureId, Material.Stone)
    {
    }

    public override int getDroppedItemId(int blockMeta) => id == CoalOre.id ? Item.Coal.id : id == DiamondOre.id ? Item.Diamond.id : id == LapisOre.id ? Item.Dye.id : id;

    public override int getDroppedItemCount() => id == LapisOre.id ? 4 + Random.Shared.Next(5) : 1;

    protected override int getDroppedItemMeta(int blockMeta) => id == LapisOre.id ? 4 : 0;
}
