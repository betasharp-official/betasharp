using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockGlowstone(int i, int j, Material material) : Block(i, j, material)
{
    public override int GetDroppedItemCount() => 2 + Random.Shared.Next(3);

    public override int GetDroppedItemId(int blockMeta) => Item.GlowstoneDust.id;
}
