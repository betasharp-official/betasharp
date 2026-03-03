using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds;

namespace BetaSharp.Blocks;

public class BlockLeavesBase : Block
{
    protected bool graphicsLevel;

    protected BlockLeavesBase(int id, int textureId, Material material, bool graphicsLevel) : base(id, textureId, material)
    {
        this.graphicsLevel = graphicsLevel;
    }

    public override bool isOpaque()
    {
        return false;
    }

    public override bool isSideVisible(IBlockAccess iBlockAccess, int x, int y, int z, int side)
    {
        int var6 = iBlockAccess.getBlockId(x, y, z);
        return !graphicsLevel && var6 == id ? false : base.isSideVisible(iBlockAccess, x, y, z, side);
    }
}
