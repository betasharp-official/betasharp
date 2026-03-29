using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockClay(int id, int textureId) : Block(id, textureId, Material.Clay)
{
    public override int DroppedItemCount => 4;
    public override int GetDroppedItemId(int blockMeta) => Item.Clay.id;
}
