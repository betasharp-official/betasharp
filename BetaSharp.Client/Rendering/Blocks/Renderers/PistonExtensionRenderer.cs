using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class PistonExtensionRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int metadata = ctx.World.GetBlockMeta(pos.x, pos.y, pos.z);
        int facing = BlockPistonExtension.getFacing(metadata);
        float luminance = block.getLuminance(ctx.World, pos.x, pos.y, pos.z);

        // Using CustomFlag to track if this is a ShortArm rendering phase
        bool isShortArm = ctx.CustomFlag;
        float armLength = isShortArm ? 1.0F : 0.5F;
        float texWidth = isShortArm ? 16.0f : 8.0f;

        int uvTop = 0, uvBottom = 0, uvNorth = 0, uvSouth = 0, uvEast = 0, uvWest = 0;
        Box? bounds = ctx.OverrideBounds ?? block.BoundingBox;

        // 1. Calculate the rotations and bounds for the "Head" of the piston
        switch (facing)
        {
            case 0: // Down (-Y)
                uvSouth = 2;
                uvNorth = 2;
                uvEast = 2;
                uvWest = 2;
                uvTop = 0;
                uvBottom = 0;
                bounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 0.25F, 1.0F);
                break;
            case 1: // Up (+Y)
                uvSouth = 0;
                uvNorth = 0;
                uvEast = 0;
                uvWest = 0;
                uvTop = 0;
                uvBottom = 0;
                bounds = new Box(0.0F, 0.75F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
            case 2: // North (-Z)
                uvSouth = 1;
                uvNorth = 3;
                uvEast = 1;
                uvWest = 0;
                uvTop = 0;
                uvBottom = 0;
                bounds = new Box(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 0.25F);
                break;
            case 3: // South (+Z)
                uvSouth = 3;
                uvNorth = 1;
                uvEast = 0;
                uvWest = 1;
                uvTop = 2;
                uvBottom = 2;
                bounds = new Box(0.0F, 0.0F, 0.75F, 1.0F, 1.0F, 1.0F);
                break;
            case 4: // West (-X)
                uvSouth = 0;
                uvNorth = 0;
                uvEast = 1;
                uvWest = 3;
                uvTop = 3;
                uvBottom = 3;
                bounds = new Box(0.0F, 0.0F, 0.0F, 0.25F, 1.0F, 1.0F);
                break;
            case 5: // East (+X)
                uvSouth = 0;
                uvNorth = 0;
                uvEast = 3;
                uvWest = 1;
                uvTop = 1;
                uvBottom = 1;
                bounds = new Box(0.75F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F);
                break;
        }

        var headCtx = ctx with
        {
            OverrideBounds = bounds,
            UvRotateTop = uvTop,
            UvRotateBottom = uvBottom,
            UvRotateNorth = uvNorth,
            UvRotateSouth = uvSouth,
            UvRotateEast = uvEast,
            UvRotateWest = uvWest
        };

        bool hasRendered = headCtx.DrawBlock(block, pos);

        // 2. Render the custom extension arm geometry
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;

        switch (facing)
        {
            case 0:
                RenderPistonArmY(ref ctx, x + 0.375f, x + 0.625f, y + 0.25f, y + 0.25f + armLength, z + 0.625f, z + 0.625f, luminance * 0.8F, texWidth);
                RenderPistonArmY(ref ctx, x + 0.625f, x + 0.375f, y + 0.25f, y + 0.25f + armLength, z + 0.375f, z + 0.375f, luminance * 0.8F, texWidth);
                RenderPistonArmY(ref ctx, x + 0.375f, x + 0.375f, y + 0.25f, y + 0.25f + armLength, z + 0.375f, z + 0.625f, luminance * 0.6F, texWidth);
                RenderPistonArmY(ref ctx, x + 0.625f, x + 0.625f, y + 0.25f, y + 0.25f + armLength, z + 0.625f, z + 0.375f, luminance * 0.6F, texWidth);
                break;
            case 1:
                RenderPistonArmY(ref ctx, x + 0.375f, x + 0.625f, y + 0.75f - armLength, y + 0.75f, z + 0.625f, z + 0.625f, luminance * 0.8F, texWidth);
                RenderPistonArmY(ref ctx, x + 0.625f, x + 0.375f, y + 0.75f - armLength, y + 0.75f, z + 0.375f, z + 0.375f, luminance * 0.8F, texWidth);
                RenderPistonArmY(ref ctx, x + 0.375f, x + 0.375f, y + 0.75f - armLength, y + 0.75f, z + 0.375f, z + 0.625f, luminance * 0.6F, texWidth);
                RenderPistonArmY(ref ctx, x + 0.625f, x + 0.625f, y + 0.75f - armLength, y + 0.75f, z + 0.625f, z + 0.375f, luminance * 0.6F, texWidth);
                break;
            case 2:
                RenderPistonArmZ(ref ctx, x + 0.375f, x + 0.375f, y + 0.625f, y + 0.375f, z + 0.25f, z + 0.25f + armLength, luminance * 0.6F, texWidth);
                RenderPistonArmZ(ref ctx, x + 0.625f, x + 0.625f, y + 0.375f, y + 0.625f, z + 0.25f, z + 0.25f + armLength, luminance * 0.6F, texWidth);
                RenderPistonArmZ(ref ctx, x + 0.375f, x + 0.625f, y + 0.375f, y + 0.375f, z + 0.25f, z + 0.25f + armLength, luminance * 0.5F, texWidth);
                RenderPistonArmZ(ref ctx, x + 0.625f, x + 0.375f, y + 0.625f, y + 0.625f, z + 0.25f, z + 0.25f + armLength, luminance, texWidth);
                break;
            case 3:
                RenderPistonArmZ(ref ctx, x + 0.375f, x + 0.375f, y + 0.625f, y + 0.375f, z + 0.75f - armLength, z + 0.75f, luminance * 0.6F, texWidth);
                RenderPistonArmZ(ref ctx, x + 0.625f, x + 0.625f, y + 0.375f, y + 0.625f, z + 0.75f - armLength, z + 0.75f, luminance * 0.6F, texWidth);
                RenderPistonArmZ(ref ctx, x + 0.375f, x + 0.625f, y + 0.375f, y + 0.375f, z + 0.75f - armLength, z + 0.75f, luminance * 0.5F, texWidth);
                RenderPistonArmZ(ref ctx, x + 0.625f, x + 0.375f, y + 0.625f, y + 0.625f, z + 0.75f - armLength, z + 0.75f, luminance, texWidth);
                break;
            case 4:
                RenderPistonArmX(ref ctx, x + 0.25f, x + 0.25f + armLength, y + 0.375f, y + 0.375f, z + 0.625f, z + 0.375f, luminance * 0.5F, texWidth);
                RenderPistonArmX(ref ctx, x + 0.25f, x + 0.25f + armLength, y + 0.625f, y + 0.625f, z + 0.375f, z + 0.625f, luminance, texWidth);
                RenderPistonArmX(ref ctx, x + 0.25f, x + 0.25f + armLength, y + 0.375f, y + 0.625f, z + 0.375f, z + 0.375f, luminance * 0.6F, texWidth);
                RenderPistonArmX(ref ctx, x + 0.25f, x + 0.25f + armLength, y + 0.625f, y + 0.375f, z + 0.625f, z + 0.625f, luminance * 0.6F, texWidth);
                break;
            case 5:
                RenderPistonArmX(ref ctx, x + 0.75f - armLength, x + 0.75f, y + 0.375f, y + 0.375f, z + 0.625f, z + 0.375f, luminance * 0.5F, texWidth);
                RenderPistonArmX(ref ctx, x + 0.75f - armLength, x + 0.75f, y + 0.625f, y + 0.625f, z + 0.375f, z + 0.625f, luminance, texWidth);
                RenderPistonArmX(ref ctx, x + 0.75f - armLength, x + 0.75f, y + 0.375f, y + 0.625f, z + 0.375f, z + 0.375f, luminance * 0.6F, texWidth);
                RenderPistonArmX(ref ctx, x + 0.75f - armLength, x + 0.75f, y + 0.625f, y + 0.375f, z + 0.625f, z + 0.625f, luminance * 0.6F, texWidth);
                break;
        }

        return hasRendered;
    }


    private void RenderPistonArmY(ref BlockRenderContext ctx, float x1, float x2, float y1,
        float y2, float z1, float z2, float luminance, float textureWidth)
    {
        int textureId = 108;

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float minU = texU / 256.0f;
        float minV = texV / 256.0f;
        float maxU = (texU + textureWidth - 0.01f) / 256.0f;
        float maxV = (texV + 4.0f - 0.01f) / 256.0f;

        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
        ctx.Tess.addVertexWithUV(x1, y2, z1, maxU, minV);
        ctx.Tess.addVertexWithUV(x1, y1, z1, minU, minV);
        ctx.Tess.addVertexWithUV(x2, y1, z2, minU, maxV);
        ctx.Tess.addVertexWithUV(x2, y2, z2, maxU, maxV);
    }

    private void RenderPistonArmZ(ref BlockRenderContext ctx, float x1, float x2, float y1,
        float y2, float z1, float z2, float luminance, float textureWidth)
    {
        int textureId = 108;

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float minU = texU / 256.0f;
        float minV = texV / 256.0f;
        float maxU = (texU + textureWidth - 0.01f) / 256.0f;
        float maxV = (texV + 4.0f - 0.01f) / 256.0f;

        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
        ctx.Tess.addVertexWithUV(x1, y1, z2, maxU, minV);
        ctx.Tess.addVertexWithUV(x1, y1, z1, minU, minV);
        ctx.Tess.addVertexWithUV(x2, y2, z1, minU, maxV);
        ctx.Tess.addVertexWithUV(x2, y2, z2, maxU, maxV);
    }

    private void RenderPistonArmX(ref BlockRenderContext ctx, float x1, float x2, float y1,
        float y2, float z1, float z2, float luminance, float textureWidth)
    {
        int textureId = 108;

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;

        float minU = texU / 256.0f;
        float minV = texV / 256.0f;
        float maxU = (texU + textureWidth - 0.01f) / 256.0f;
        float maxV = (texV + 4.0f - 0.01f) / 256.0f;

        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
        ctx.Tess.addVertexWithUV(x2, y1, z1, maxU, minV);
        ctx.Tess.addVertexWithUV(x1, y1, z1, minU, minV);
        ctx.Tess.addVertexWithUV(x1, y2, z2, minU, maxV);
        ctx.Tess.addVertexWithUV(x2, y2, z2, maxU, maxV);
    }
}
