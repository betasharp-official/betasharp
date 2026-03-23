using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class StairsRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        bool hasRendered = false;
        int direction = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);


        var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.5F) };
        hasRendered |= upperCtx.DrawBlock(block, pos);

        // Lower step (South half)
        var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.5F, 1.0F, 0.5F, 1.0F) };
        hasRendered |= lowerCtx.DrawBlock(block, pos);


        // Notice: No cleanup required!
        // The original context remains untouched and the sub-contexts just fall out of scope.

        return hasRendered;
    }
}
