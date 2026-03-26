using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class StairsRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        bool hasRendered = false;
        int direction = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);

        if (ctx.BlockReader is ItemRenderBlockAccess)
        {
            direction = 3;
        }

        switch (direction)
        {
            case 0:
                {
                    var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 0.5F, 0.5F, 1.0F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);

                    var upperCtx = ctx with { OverrideBounds = new Box(0.5F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);
                    break;
                }
            case 1:
                {
                    var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 0.5F, 1.0F, 1.0F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);

                    var lowerCtx = ctx with { OverrideBounds = new Box(0.5F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);
                    break;
                }
            case 2:
                {
                    var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 0.5F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);

                    var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.5F, 1.0F, 1.0F, 1.0F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);
                    break;
                }
            case 3:
                {

                    var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.5F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);

                    var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.5F, 1.0F, 0.5F, 1.0F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);
                    break;
                }
        }

        return hasRendered;
    }
}
