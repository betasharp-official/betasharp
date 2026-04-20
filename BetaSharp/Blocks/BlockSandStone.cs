using BetaSharp.Blocks.Materials;

namespace BetaSharp.Blocks;

internal class BlockSandStone(int id) : Block(id, BlockTextures.SandstoneSide, Material.Stone)
{
    public override int GetTexture(Side side) =>
        side switch
        {
            Side.Up => BlockTextures.SandstoneTop,
            Side.Down => BlockTextures.SandstoneBottom,
            _ => BlockTextures.SandstoneSide
        };
}
