using BetaSharp.Blocks;

namespace BetaSharp.Items;

internal class ItemSpade(int id, ToolMaterial toolMaterial) : ItemTool(id, 1, toolMaterial, s_blocksEffectiveAgainst)
{
    private static readonly Block[] s_blocksEffectiveAgainst =
    [
        Block.GrassBlock,
        Block.Dirt,
        Block.Sand,
        Block.Gravel,
        Block.Snow,
        Block.SnowBlock,
        Block.Clay,
        Block.Farmland
    ];

    public override bool IsSuitableFor(Block block) => block == Block.Snow || block == Block.SnowBlock;
}
