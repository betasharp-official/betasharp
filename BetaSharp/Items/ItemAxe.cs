using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemAxe(int id, ToolMaterial toolMaterial) : ItemTool(id, 3, toolMaterial, s_blocksEffectiveAgainst)
{
    private static readonly Block[] s_blocksEffectiveAgainst =
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
}
