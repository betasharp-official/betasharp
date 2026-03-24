using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockClay : Block
{
    public BlockClay(int id, int textureId) : base(id, textureId, Material.Clay)
    {
    }

    public override int getDroppedItemId(int blockMeta)
    {
        return Item.Clay.id;
    }

    public override int getDroppedItemCount()
    {
        return 4;
    }
}
