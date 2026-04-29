using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockClay(int id, int textureId) : Block(id, textureId, Material.Clay)
{
    public override int getDroppedItemId(int blockMeta) => Item.Clay.Id;

    public override int getDroppedItemCount() => 4;
}
