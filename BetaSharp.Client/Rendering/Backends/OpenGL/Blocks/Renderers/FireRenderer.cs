using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class FireRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int textureId = block.GetTexture(0);
        if (ctx.OverrideTexture >= 0) textureId = ctx.OverrideTexture;

        float luminance = block.getLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;
        float minU = texU / 256.0F;
        float maxU = (texU + 15.99F) / 256.0F;
        float minV = texV / 256.0F;
        float maxV = (texV + 15.99F) / 256.0F;

        float fireHeight = 1.4F;

        // If not on a solid/flammable floor, render climbing flames on walls
        if (!ctx.BlockReader.ShouldSuffocate(pos.x, pos.y - 1, pos.z) &&
            !Block.Fire.isFlammable(ctx.BlockReader, pos.x, pos.y - 1, pos.z))
        {
            float sideInset = 0.2F;
            float yOffset = 1.0F / 16.0F;

            // Variation: Flip texture or use second fire frame based on position
            if ((pos.x + pos.y + pos.z & 1) == 1)
            {
                minV = (texV + 16) / 256.0F;
                maxV = (texV + 15.99F + 16.0F) / 256.0F;
            }

            if ((pos.x / 2 + pos.y / 2 + pos.z / 2 & 1) == 1)
            {
                (minU, maxU) = (maxU, minU);
            }

            // Climbing West Wall
            if (Block.Fire.isFlammable(ctx.BlockReader, pos.x - 1, pos.y, pos.z))
            {
                ctx.Tess.addVertexWithUV(pos.x + sideInset, pos.y + fireHeight + yOffset, pos.z + 1, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + sideInset, pos.y + fireHeight + yOffset, pos.z, minU, minV);
                // Backface
                ctx.Tess.addVertexWithUV(pos.x + sideInset, pos.y + fireHeight + yOffset, pos.z, minU, minV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + sideInset, pos.y + fireHeight + yOffset, pos.z + 1, maxU, minV);
            }

            // Climbing East Wall
            if (Block.Fire.isFlammable(ctx.BlockReader, pos.x + 1, pos.y, pos.z))
            {
                ctx.Tess.addVertexWithUV(pos.x + 1 - sideInset, pos.y + fireHeight + yOffset, pos.z, minU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1 - sideInset, pos.y + fireHeight + yOffset, pos.z + 1, maxU, minV);
                // Backface
                ctx.Tess.addVertexWithUV(pos.x + 1 - sideInset, pos.y + fireHeight + yOffset, pos.z + 1, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1 - sideInset, pos.y + fireHeight + yOffset, pos.z, minU, minV);
            }

            // Climbing North Wall
            if (Block.Fire.isFlammable(ctx.BlockReader, pos.x, pos.y, pos.z - 1))
            {
                ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight + yOffset, pos.z + sideInset, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight + yOffset, pos.z + sideInset, minU, minV);
                // Backface
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight + yOffset, pos.z + sideInset, minU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight + yOffset, pos.z + sideInset, maxU, minV);
            }

            // Climbing South Wall
            if (Block.Fire.isFlammable(ctx.BlockReader, pos.x, pos.y, pos.z + 1))
            {
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight + yOffset, pos.z + 1 - sideInset, minU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z + 1, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight + yOffset, pos.z + 1 - sideInset, maxU, minV);
                // Backface
                ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight + yOffset, pos.z + 1 - sideInset, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x, pos.y + yOffset, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + yOffset, pos.z + 1, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight + yOffset, pos.z + 1 - sideInset, minU, minV);
            }

            // Climbing Ceilings
            if (Block.Fire.isFlammable(ctx.BlockReader, pos.x, pos.y + 1, pos.z))
            {
                float xMax = pos.x + 1, xMin = pos.x;
                float zMax = pos.z + 1, zMin = pos.z;

                minU = texU / 256.0F;
                maxU = (texU + 15.99F) / 256.0F;
                minV = texV / 256.0F;
                maxV = (texV + 15.99F) / 256.0F;

                int ceilY = pos.y + 1;
                float ceilOffset = -0.2F;

                if ((pos.x + ceilY + pos.z & 1) == 0)
                {
                    ctx.Tess.addVertexWithUV(xMin, ceilY + ceilOffset, pos.z, maxU, minV);
                    ctx.Tess.addVertexWithUV(xMax, ceilY, pos.z, maxU, maxV);
                    ctx.Tess.addVertexWithUV(xMax, ceilY, pos.z + 1, minU, maxV);
                    ctx.Tess.addVertexWithUV(xMin, ceilY + ceilOffset, pos.z + 1, minU, minV);

                    minV = (texV + 16) / 256.0F;
                    maxV = (texV + 15.99F + 16.0F) / 256.0F;

                    ctx.Tess.addVertexWithUV(xMax, ceilY + ceilOffset, pos.z + 1, maxU, minV);
                    ctx.Tess.addVertexWithUV(xMin, ceilY, pos.z + 1, maxU, maxV);
                    ctx.Tess.addVertexWithUV(xMin, ceilY, pos.z, minU, maxV);
                    ctx.Tess.addVertexWithUV(xMax, ceilY + ceilOffset, pos.z, minU, minV);
                }
                else
                {
                    ctx.Tess.addVertexWithUV(pos.x, ceilY + ceilOffset, zMax, maxU, minV);
                    ctx.Tess.addVertexWithUV(pos.x, ceilY, zMin, maxU, maxV);
                    ctx.Tess.addVertexWithUV(pos.x + 1, ceilY, zMin, minU, maxV);
                    ctx.Tess.addVertexWithUV(pos.x + 1, ceilY + ceilOffset, zMax, minU, minV);

                    minV = (texV + 16) / 256.0F;
                    maxV = (texV + 15.99F + 16.0F) / 256.0F;

                    ctx.Tess.addVertexWithUV(pos.x + 1, ceilY + ceilOffset, zMin, maxU, minV);
                    ctx.Tess.addVertexWithUV(pos.x + 1, ceilY, zMax, maxU, maxV);
                    ctx.Tess.addVertexWithUV(pos.x, ceilY, zMax, minU, maxV);
                    ctx.Tess.addVertexWithUV(pos.x, ceilY + ceilOffset, zMin, minU, minV);
                }
            }
        }
        else // Render central "X" flames for fire on solid floors
        {
            float insetSmall = 0.2f, insetLarge = 0.3f;
            float xC = pos.x + 0.5f, zC = pos.z + 0.5f;

            // First diagonal set
            ctx.Tess.addVertexWithUV(xC - insetLarge, pos.y + fireHeight, pos.z + 1, maxU, minV);
            ctx.Tess.addVertexWithUV(xC + insetSmall, pos.y, pos.z + 1, maxU, maxV);
            ctx.Tess.addVertexWithUV(xC + insetSmall, pos.y, pos.z, minU, maxV);
            ctx.Tess.addVertexWithUV(xC - insetLarge, pos.y + fireHeight, pos.z, minU, minV);

            ctx.Tess.addVertexWithUV(xC + insetLarge, pos.y + fireHeight, pos.z, maxU, minV);
            ctx.Tess.addVertexWithUV(xC - insetSmall, pos.y, pos.z, maxU, maxV);
            ctx.Tess.addVertexWithUV(xC - insetSmall, pos.y, pos.z + 1, minU, maxV);
            ctx.Tess.addVertexWithUV(xC + insetLarge, pos.y + fireHeight, pos.z + 1, minU, minV);

            // Switch texture frame
            minV = (texV + 16) / 256.0F;
            maxV = (texV + 15.99F + 16.0F) / 256.0F;

            // Second diagonal set (X-axis dominant)
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight, zC + insetLarge, maxU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, zC - insetSmall, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x, pos.y, zC - insetSmall, minU, maxV);
            ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight, zC + insetLarge, minU, minV);

            ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight, zC - insetLarge, maxU, minV);
            ctx.Tess.addVertexWithUV(pos.x, pos.y, zC + insetSmall, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, zC + insetSmall, minU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight, zC - insetLarge, minU, minV);

            // Third set (outer crossing)
            float i4 = 0.4f, i5 = 0.5f;
            ctx.Tess.addVertexWithUV(xC - i4, pos.y + fireHeight, pos.z, minU, minV);
            ctx.Tess.addVertexWithUV(xC - i5, pos.y, pos.z, minU, maxV);
            ctx.Tess.addVertexWithUV(xC - i5, pos.y, pos.z + 1, maxU, maxV);
            ctx.Tess.addVertexWithUV(xC - i4, pos.y + fireHeight, pos.z + 1, maxU, minV);

            ctx.Tess.addVertexWithUV(xC + i4, pos.y + fireHeight, pos.z + 1, minU, minV);
            ctx.Tess.addVertexWithUV(xC + i5, pos.y, pos.z + 1, minU, maxV);
            ctx.Tess.addVertexWithUV(xC + i5, pos.y, pos.z, maxU, maxV);
            ctx.Tess.addVertexWithUV(xC + i4, pos.y + fireHeight, pos.z, maxU, minV);

            // Final set
            minV = texV / 256.0F;
            maxV = (texV + 15.99F) / 256.0F;
            ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight, zC + i4, minU, minV);
            ctx.Tess.addVertexWithUV(pos.x, pos.y, zC + i5, minU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, zC + i5, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight, zC + i4, maxU, minV);

            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y + fireHeight, zC - i4, minU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, zC - i5, minU, maxV);
            ctx.Tess.addVertexWithUV(pos.x, pos.y, zC - i5, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x, pos.y + fireHeight, zC - i4, maxU, minV);
        }

        return true;
    }
}
