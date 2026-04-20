using BetaSharp.Blocks.Materials;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Blocks;

public class BlockLeavesBase : Block
{
    protected bool GraphicsLevel;

    protected BlockLeavesBase(int id, int textureId, Material material, bool graphicsLevel) : base(id, textureId, material) => GraphicsLevel = graphicsLevel;

    public override bool IsOpaque() => false;

    public override bool IsSideVisible(IBlockReader iBlockReader, int x, int y, int z, Side side)
    {
        int blockId = iBlockReader.GetBlockId(x, y, z);
        return (GraphicsLevel || blockId != ID) && base.IsSideVisible(iBlockReader, x, y, z, side);
    }
}
