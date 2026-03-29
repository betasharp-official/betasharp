using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemAxe : ItemTool
{

    private static Block[] blocksEffectiveAgainst =
    [
        Block.Planks,
        Block.Bookshelf,
        Block.Log,
        Block.Chest,
        Block.CraftingTable,
        Block.WoodenStairs,
        Block.Ladder,
        Block.Trapdoor,
        Block.Fence
    ];

    public ItemAxe(int id, EnumToolMaterial enumToolMaterial) : base(id, 3, enumToolMaterial, blocksEffectiveAgainst)
    {
    }
}
