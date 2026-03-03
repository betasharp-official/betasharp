using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class PistonBaseRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int metadata = ctx.World.getBlockMeta(pos.x, pos.y, pos.z);
        bool isExpanded = ctx.CustomFlag || (metadata & 8) != 0;
        int facing = BlockPistonBase.getFacing(metadata);

        int uvTop = 0, uvBottom = 0, uvNorth = 0, uvSouth = 0, uvEast = 0, uvWest = 0;
        Box? bounds = ctx.OverrideBounds ?? block.BoundingBox;

        switch (facing)
        {
            case 0: // Down (-Y)
                // Set to 0 so the texture samples from the correct height!
                uvSouth = 2; uvNorth = 2; uvEast = 2; uvWest = 2;
                uvTop = 0; uvBottom = 0;
                if (isExpanded) bounds = new Box(0.0F, 0.25F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 1: // Up (+Y)
                uvSouth = 0; uvNorth = 0; uvEast = 0; uvWest = 0;
                uvTop = 0; uvBottom = 0;
                if (isExpanded) bounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 0.75F, 1.0F);
                break;
            case 2: // North (-Z)
                uvSouth = 1; uvNorth = 3; uvEast = 1; uvWest = 0;
                uvTop = 0; uvBottom = 0;
                if (isExpanded) bounds = new Box(0.0F, 0.0F, 0.25F, 1.0F, 1.0F, 1.0F);
                break;
            case 3: // South (+Z)
                uvSouth = 3; uvNorth = 1; uvEast = 0; uvWest = 1;
                uvTop = 2; uvBottom = 2;
                if (isExpanded) bounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.75F);
                break;
            case 4: // West (-X)
                uvSouth = 0; uvNorth = 0; uvEast = 1; uvWest = 3;
                uvTop = 3; uvBottom = 3;
                if (isExpanded) bounds = new Box(0.25F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 5: // East (+X)
                uvSouth = 0; uvNorth = 0; uvEast = 3; uvWest = 1;
                uvTop = 1; uvBottom = 1;
                if (isExpanded) bounds = new Box(0.0F, 0.0F, 0.0F, 0.75F, 1.0F, 1.0F);
                break;
        }

        var baseCtx = ctx with
        {
            OverrideBounds = bounds,
            UvRotateTop = uvTop,
            UvRotateBottom = uvBottom,
            UvRotateNorth = uvNorth,
            UvRotateSouth = uvSouth,
            UvRotateEast = uvEast,
            UvRotateWest = uvWest
        };

        return baseCtx.DrawBlock(block, pos);
    }
}
