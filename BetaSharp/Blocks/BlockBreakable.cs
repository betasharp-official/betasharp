using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockBreakable : Block
{
    private readonly bool _hideAdjacentFaces;

    protected BlockBreakable(int id, int textureId, Material material, bool hideAdjacentFaces) : base(id, textureId, material) => _hideAdjacentFaces = hideAdjacentFaces;

    public override bool isOpaque() => false;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        int neighborBlockId = iBlockReader.GetBlockId(x, y, z);
        return (_hideAdjacentFaces || neighborBlockId != id) && base.isSideVisible(iBlockReader, x, y, z, side);
    }
}
