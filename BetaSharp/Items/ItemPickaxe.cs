using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;

namespace BetaSharp.Items;

internal class ItemPickaxe : ItemTool
{

    private static Block[] blocksEffectiveAgainst =
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

    public ItemPickaxe(int id, ToolMaterial toolMaterial) : base(id, 2, toolMaterial, blocksEffectiveAgainst)
    {
    }

    public override bool isSuitableFor(Block block)
    {
        return block == Block.Obsidian ? toolMaterial.getHarvestLevel() == 3 : (block != Block.DiamondBlock && block != Block.DiamondOre ? (block != Block.GoldBlock && block != Block.GoldOre ? (block != Block.IronBlock && block != Block.IronOre ? (block != Block.LapisBlock && block != Block.LapisOre ? (block != Block.RedstoneOre && block != Block.LitRedstoneOre ? (block.material == Material.Stone ? true : block.material == Material.Metal) : toolMaterial.getHarvestLevel() >= 2) : toolMaterial.getHarvestLevel() >= 1) : toolMaterial.getHarvestLevel() >= 1) : toolMaterial.getHarvestLevel() >= 2) : toolMaterial.getHarvestLevel() >= 2);
    }
}
