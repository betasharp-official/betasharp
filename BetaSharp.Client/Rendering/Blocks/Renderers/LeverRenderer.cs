using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class LeverRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int metadata = ctx.World.getBlockMeta(pos.x, pos.y, pos.z);
        int orientation = metadata & 7;
        bool isActivated = (metadata & 8) > 0;

        float baseWidth = 0.25F;
        float baseThickness = 3.0F / 16.0F;
        float baseHeight = 3.0F / 16.0F;

        // 1. Calculate the baseplate box (Your existing logic)
        Box baseBox = orientation switch
        {
            5 => new Box(0.5 - baseHeight, 0.0, 0.5 - baseWidth, 0.5 + baseHeight, baseThickness, 0.5 + baseWidth),
            6 => new Box(0.5 - baseWidth, 0.0, 0.5 - baseHeight, 0.5 + baseWidth, baseThickness, 0.5 + baseHeight),
            4 => new Box(0.5 - baseHeight, 0.5 - baseWidth, 1.0 - baseThickness, 0.5 + baseHeight, 0.5 + baseWidth,
                1.0),
            3 => new Box(0.5 - baseHeight, 0.5 - baseWidth, 0.0, 0.5 + baseHeight, 0.5 + baseWidth, baseThickness),
            2 => new Box(1.0 - baseThickness, 0.5 - baseWidth, 0.5 - baseHeight, 1.0, 0.5 + baseWidth,
                0.5 + baseHeight),
            1 => new Box(0.0, 0.5 - baseWidth, 0.5 - baseHeight, baseThickness, 0.5 + baseWidth, 0.5 + baseHeight),
            _ => new Box(0, 0, 0, 1, 1, 1)
        };

        // Levers use a cobblestone texture for the baseplate by default, unless overridden
        int baseTextureId = ctx.OverrideTexture >= 0 ? ctx.OverrideTexture : Block.Cobblestone.textureId;

        // Create a sub-context specifically for drawing the baseplate
        var baseCtx = new BlockRenderContext(
            world: ctx.World,
            tess: ctx.Tess,
            overrideTexture: baseTextureId,
            renderAllFaces: ctx.RenderAllFaces,
            flipTexture: ctx.FlipTexture,
            bounds: baseBox,
            uvTop: ctx.UvRotateTop,
            uvBottom: ctx.UvRotateBottom,
            uvNorth: ctx.UvRotateNorth,
            uvSouth: ctx.UvRotateSouth,
            uvEast: ctx.UvRotateEast,
            uvWest: ctx.UvRotateWest,
            customFlag: ctx.CustomFlag,
            enableAo: false,
            aoBlendMode: 1
        );

        // Draw the base using the helper
        baseCtx.DrawBlock(block, pos);

        var handleCtx = new BlockRenderContext(
            world: ctx.World,
            tess: ctx.Tess,
            overrideTexture: ctx.OverrideTexture,
            renderAllFaces: ctx.RenderAllFaces,
            flipTexture: ctx.FlipTexture,
            bounds: null,
            uvTop: ctx.UvRotateTop,
            uvBottom: ctx.UvRotateBottom,
            uvNorth: ctx.UvRotateNorth,
            uvSouth: ctx.UvRotateSouth,
            uvEast: ctx.UvRotateEast,
            uvWest: ctx.UvRotateWest,
            customFlag: ctx.CustomFlag,
            enableAo: false,
            aoBlendMode: 1
        );

        // Determine texture for the handle itself
        int handleTextureId = handleCtx.OverrideTexture >= 0 ? handleCtx.OverrideTexture : block.getTexture(0);

        int texU = (handleTextureId & 15) << 4;
        int texV = handleTextureId & 240;
        float minU = texU / 256.0F;
        float maxU = (texU + 15.99F) / 256.0F;
        float minV = texV / 256.0F;
        float maxV = (texV + 15.99F) / 256.0F;

        // --- 3. Handle Vertex Math ---
        Vec3D[] vertices = new Vec3D[8];
        float hRadius = 1.0F / 16.0F;
        float hLength = 10.0F / 16.0F;

        // Initial handle box (standing straight up)
        vertices[0] = new Vec3D(-hRadius, 0.0D, -hRadius);
        vertices[1] = new Vec3D(hRadius, 0.0D, -hRadius);
        vertices[2] = new Vec3D(hRadius, 0.0D, hRadius);
        vertices[3] = new Vec3D(-hRadius, 0.0D, hRadius);
        vertices[4] = new Vec3D(-hRadius, hLength, -hRadius);
        vertices[5] = new Vec3D(hRadius, hLength, -hRadius);
        vertices[6] = new Vec3D(hRadius, hLength, hRadius);
        vertices[7] = new Vec3D(-hRadius, hLength, hRadius);

        for (int i = 0; i < 8; ++i)
        {
            // Toggle angle based on state
            if (isActivated)
            {
                vertices[i].z -= 1.0D / 16.0D;
                vertices[i].rotateAroundX((float)Math.PI * 2.0F / 9.0F);
            }
            else
            {
                vertices[i].z += 1.0D / 16.0D;
                vertices[i].rotateAroundX(-(float)Math.PI * 2.0F / 9.0F);
            }

            // Apply orientation rotations
            if (orientation == 6) vertices[i].rotateAroundY((float)Math.PI * 0.5F);

            if (orientation < 5) // Wall mount requires extra rotation
            {
                vertices[i].y -= 0.375D;
                vertices[i].rotateAroundX((float)Math.PI * 0.5F);

                if (orientation == 3) vertices[i].rotateAroundY((float)Math.PI);
                if (orientation == 2) vertices[i].rotateAroundY((float)Math.PI * 0.5F);
                if (orientation == 1) vertices[i].rotateAroundY((float)Math.PI * -0.5F);

                vertices[i].x += pos.x + 0.5D; // Fixed .X to .x
                vertices[i].y += pos.y + 0.5D;
                vertices[i].z += pos.z + 0.5D;
            }
            else
            {
                vertices[i].x += pos.x + 0.5D; // Fixed .X to .x
                vertices[i].y += pos.y + 2.0F / 16.0F;
                vertices[i].z += pos.z + 0.5D;
            }
        }

        // --- 4. Draw the Handle Faces ---
        for (int face = 0; face < 6; ++face)
        {
            // The handle uses specific tiny snippets of the texture atlas for its detail
            if (face == 0) // Bottom cap
            {
                minU = (texU + 7) / 256.0F;
                maxU = (texU + 9 - 0.01F) / 256.0F;
                minV = (texV + 6) / 256.0F;
                maxV = (texV + 8 - 0.01F) / 256.0F;
            }
            else if (face == 2) // Side detail
            {
                minU = (texU + 7) / 256.0F;
                maxU = (texU + 9 - 0.01F) / 256.0F;
                minV = (texV + 6) / 256.0F;
                maxV = (texV + 16 - 0.01F) / 256.0F;
            }

            Vec3D v1 = default, v2 = default, v3 = default, v4 = default;

            switch (face)
            {
                case 0:
                    v1 = vertices[0];
                    v2 = vertices[1];
                    v3 = vertices[2];
                    v4 = vertices[3];
                    break;
                case 1:
                    v1 = vertices[7];
                    v2 = vertices[6];
                    v3 = vertices[5];
                    v4 = vertices[4];
                    break;
                case 2:
                    v1 = vertices[1];
                    v2 = vertices[0];
                    v3 = vertices[4];
                    v4 = vertices[5];
                    break;
                case 3:
                    v1 = vertices[2];
                    v2 = vertices[1];
                    v3 = vertices[5];
                    v4 = vertices[6];
                    break;
                case 4:
                    v1 = vertices[3];
                    v2 = vertices[2];
                    v3 = vertices[6];
                    v4 = vertices[7];
                    break;
                case 5:
                    v1 = vertices[0];
                    v2 = vertices[3];
                    v3 = vertices[7];
                    v4 = vertices[4];
                    break;
            }

            handleCtx.Tess.addVertexWithUV(v1.x, v1.y, v1.z, minU, maxV);
            handleCtx.Tess.addVertexWithUV(v2.x, v2.y, v2.z, maxU, maxV);
            handleCtx.Tess.addVertexWithUV(v3.x, v3.y, v3.z, maxU, minV);
            handleCtx.Tess.addVertexWithUV(v4.x, v4.y, v4.z, minU, minV);
        }

        return true;
    }
}
