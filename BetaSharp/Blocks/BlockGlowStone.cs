using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockGlowstone : Block
{
    public BlockGlowstone(int i, int j, Material material) : base(i, j, material)
    {
    }

    public override int getDroppedItemCount() => 2 + Random.Shared.Next(3);

    public override int getDroppedItemId(int blockMeta) => Item.GlowstoneDust.id;
}
