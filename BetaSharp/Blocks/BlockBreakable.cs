using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

// dont know what to document this as.
public class BlockBreakable : Block
{
    private readonly bool _hideAdjacentFaces;

    protected BlockBreakable(int id, int textureId, Material material, bool hideAdjacentFaces) : base(id, textureId, material) => _hideAdjacentFaces = hideAdjacentFaces;

    public override bool IsOpaque() => false;

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        int neighborBlockId = iBlockReader.GetBlockId(x, y, z);
        return (_hideAdjacentFaces || neighborBlockId != ID) && base.IsSideVisible(iBlockReader, x, y, z, side);
    }
}
