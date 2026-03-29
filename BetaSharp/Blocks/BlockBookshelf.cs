using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockBookshelf(int id, int textureId) : Block(id, textureId, Material.Wood)
{
    public override int GetTexture(Side side) => side <= Side.Up ? 4 : TextureId; // 4 is the wood plank texture id

    public override int DroppedItemCount => 0;
}
