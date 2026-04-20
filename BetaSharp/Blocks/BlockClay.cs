using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockClay(int id, int textureId) : Block(id, textureId, Material.Clay)
{
    public override int GetDroppedItemId(int blockMeta) => Item.Clay.id;

    public override int GetDroppedItemCount() => 4;
}
