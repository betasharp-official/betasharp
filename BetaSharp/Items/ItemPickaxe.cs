using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Items;

internal class ItemPickaxe(int id, ToolMaterial toolMaterial) : ItemTool(id, 2, toolMaterial, s_blocksEffectiveAgainst)
{
    private static readonly Block[] s_blocksEffectiveAgainst =
    [
        Block.Cobblestone,
        Block.DoubleSlab,
        Block.Slab,
        Block.Stone,
        Block.Sandstone,
        Block.MossyCobblestone,
        Block.IronOre,
        Block.IronBlock,
        Block.CoalOre,
        Block.GoldBlock,
        Block.GoldOre,
        Block.DiamondOre,
        Block.DiamondBlock,
        Block.Ice,
        Block.Netherrack,
        Block.LapisOre,
        Block.LapisBlock,
        Block.RedstoneOre,
        Block.CobblestoneStairs
    ];

    public override bool IsSuitableFor(Block block)
    {
        if (block == Block.Obsidian)
        {
            return ToolMaterial.getHarvestLevel() == 3;
        }

        if (block == Block.DiamondBlock || block == Block.DiamondOre || block == Block.GoldBlock || block == Block.GoldOre)
        {
            return ToolMaterial.getHarvestLevel() >= 2;
        }

        if (block == Block.IronBlock || block == Block.IronOre)
        {
            return ToolMaterial.getHarvestLevel() >= 1;
        }

        if (block != Block.LapisBlock && block != Block.LapisOre)
        {
            return block != Block.RedstoneOre && block != Block.LitRedstoneOre ? block.material == Material.Stone || block.material == Material.Metal : ToolMaterial.getHarvestLevel() >= 2;
        }

        return ToolMaterial.getHarvestLevel() >= 1;

    }
}
