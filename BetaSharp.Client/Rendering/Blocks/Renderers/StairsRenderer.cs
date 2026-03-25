using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class StairsRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        bool hasRendered = false;
        int direction = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);

        switch (direction)
        {
            // Ascending East (Stairs face West)
            case 0:
                {
                    // Lower step (West half)
                    var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 0.5F, 0.5F, 1.0F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);

                    // Upper step (East half)
                    var upperCtx = ctx with { OverrideBounds = new Box(0.5F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);
                    break;
                }
            // Ascending West (Stairs face East)
            case 1:
                {
                    // Upper step (West half)
                    var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 0.5F, 1.0F, 1.0F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);

                    // Lower step (East half)
                    var lowerCtx = ctx with { OverrideBounds = new Box(0.5F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);
                    break;
                }
            // Ascending South (Stairs face North)
            case 2:
                {
                    // Lower step (North half)
                    var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 0.5F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);

                    // Upper step (South half)
                    var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.5F, 1.0F, 1.0F, 1.0F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);
                    break;
                }
            // Ascending North (Stairs face South)
            case 3:
                {
                    // Upper step (North half)
                    var upperCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.5F) };
                    hasRendered |= upperCtx.DrawBlock(block, pos);

                    // Lower step (South half)
                    var lowerCtx = ctx with { OverrideBounds = new Box(0.0F, 0.0F, 0.5F, 1.0F, 0.5F, 1.0F) };
                    hasRendered |= lowerCtx.DrawBlock(block, pos);
                    break;
                }
        }

        // Notice: No cleanup required!
        // The original context remains untouched and the sub-contexts just fall out of scope.

        return hasRendered;
    }
}
