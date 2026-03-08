using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

public class BlockBreakable : Block
{
    private readonly bool hideAdjacentFaces;

    protected BlockBreakable(int id, int textureId, Material material, bool hideAdjacentFaces) : base(id, textureId, material) => this.hideAdjacentFaces = hideAdjacentFaces;

    public override bool isOpaque() => false;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        int neighborBlockId = iBlockReader.GetBlockId(x, y, z);
        return !hideAdjacentFaces && neighborBlockId == id ? false : base.isSideVisible(iBlockReader, x, y, z, side);
    }
}
