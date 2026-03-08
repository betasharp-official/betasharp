using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockSandStone : Block
{
    public BlockSandStone(int id) : base(id, 192, Material.Stone)
    {
    }

    public override int getTexture(int side) => side == 1 ? textureId - 16 : side == 0 ? textureId + 16 : textureId;
}
