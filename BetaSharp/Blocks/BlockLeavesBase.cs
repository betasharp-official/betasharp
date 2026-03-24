using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockLeavesBase : Block
{
    protected bool graphicsLevel;

    protected BlockLeavesBase(int id, int textureId, Material material, bool graphicsLevel) : base(id, textureId, material) => this.graphicsLevel = graphicsLevel;

    public override bool isOpaque() => false;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, int side)
    {
        int var6 = iBlockReader.GetBlockId(x, y, z);
        return !graphicsLevel && var6 == id ? false : base.isSideVisible(iBlockReader, x, y, z, side);
    }
}
