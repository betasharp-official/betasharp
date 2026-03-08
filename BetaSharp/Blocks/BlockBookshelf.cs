using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockBookshelf : Block
{
    public BlockBookshelf(int id, int textureId) : base(id, textureId, Material.Wood)
    {
    }

    public override int getTexture(int side) => side <= 1 ? 4 : textureId;

    public override int getDroppedItemCount() => 0;
}
