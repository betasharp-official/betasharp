using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockOreStorage : Block
{
    public BlockOreStorage(int id, int textureId) : base(id, Material.Metal) => this.textureId = textureId;

    public override int getTexture(int side) => textureId;
}
