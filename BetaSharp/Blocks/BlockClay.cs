using BetaSharp.Blocks.Materials;
using BetaSharp.Items;

namespace BetaSharp.Blocks;

internal class BlockClay : Block
{
    public override FaceVarianceFlags TextureVarianceFlags => FaceVarianceFlags.All;
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
