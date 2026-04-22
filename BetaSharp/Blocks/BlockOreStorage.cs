using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockOreStorage : Block
{
    public BlockOreStorage(int id, int textureId) : base(id, Material.Metal) => TextureId = textureId;

    public override int GetTexture(Side side) => TextureId;
}
