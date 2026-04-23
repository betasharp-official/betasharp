using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemSpade : ItemTool
{

    private static Block[] blocksEffectiveAgainst =
    [
        Block.GrassBlock,
        Block.Dirt,
        Block.Sand,
        Block.Gravel,
        Block.Snow,
        Block.SnowBlock,
        Block.Clay,
        Block.Farmland,
    ];

    public ItemSpade(int id, ToolMaterial toolMaterial) : base(id, 1, toolMaterial, blocksEffectiveAgainst)
    {
    }

    public override bool isSuitableFor(Block block)
    {
        return block == Block.Snow ? true : block == Block.SnowBlock;
    }
}
