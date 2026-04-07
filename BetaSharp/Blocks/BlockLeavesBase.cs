using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockLeavesBase : Block
{
    protected bool GraphicsLevel;

    protected BlockLeavesBase(int id, int textureId, Material material, bool graphicsLevel) : base(id, textureId, material) => GraphicsLevel = graphicsLevel;

    public override bool isOpaque() => false;

    public override bool isSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        int blockId = iBlockReader.GetBlockId(x, y, z);
        return (GraphicsLevel || blockId != id) && base.isSideVisible(iBlockReader, x, y, z, side);
    }
}
