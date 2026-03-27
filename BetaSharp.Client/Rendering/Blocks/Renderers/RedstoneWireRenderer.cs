using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class RedstoneWireRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int powerLevel = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);

        int textureId = block.getTexture(1, powerLevel);
        if (ctx.OverrideTexture >= 0) textureId = ctx.OverrideTexture;

        // --- 1. Calculate the Glow Color ---
        float luminance = block.getLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
        float powerPercent = powerLevel / 15.0F;

        // Red component increases with power
        float r = powerPercent * 0.6F + 0.4F;
        if (powerLevel == 0) r = 0.3F;

        // Green and Blue are much lower to keep it red, but they curve up slightly at high power
        float g = powerPercent * powerPercent * 0.7F - 0.5F;
        float b = powerPercent * powerPercent * 0.6F - 0.7F;
        if (g < 0.0F) g = 0.0F;
        if (b < 0.0F) b = 0.0F;

        ctx.Tess.setColorOpaque_F(luminance * r, luminance * g, luminance * b);

        // --- 2. UV Mapping ---
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;
        float minU = texU / 256.0F;
        float maxU = (texU + 15.99F) / 256.0F;
        float minV = texV / 256.0F;
        float maxV = (texV + 15.99F) / 256.0F;

        // --- 3. Connection Logic ---
        bool connectsWest = BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x - 1, pos.y, pos.z, 1) ||
                            (!ctx.BlockReader.ShouldSuffocate(pos.x - 1, pos.y, pos.z) &&
                             BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x - 1, pos.y - 1, pos.z, -1));
        bool connectsEast = BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x + 1, pos.y, pos.z, 3) ||
                            (!ctx.BlockReader.ShouldSuffocate(pos.x + 1, pos.y, pos.z) &&
                             BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x + 1, pos.y - 1, pos.z, -1));
        bool connectsNorth = BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x, pos.y, pos.z - 1, 2) ||
                             (!ctx.BlockReader.ShouldSuffocate(pos.x, pos.y, pos.z - 1) &&
                              BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x, pos.y - 1, pos.z - 1, -1));
        bool connectsSouth = BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x, pos.y, pos.z + 1, 0) ||
                             (!ctx.BlockReader.ShouldSuffocate(pos.x, pos.y, pos.z + 1) &&
                              BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x, pos.y - 1, pos.z + 1, -1));

        if (!ctx.BlockReader.ShouldSuffocate(pos.x, pos.y + 1, pos.z))
        {
            if (ctx.BlockReader.ShouldSuffocate(pos.x - 1, pos.y, pos.z) &&
                BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x - 1, pos.y + 1, pos.z, -1))
                connectsWest = true;
            if (ctx.BlockReader.ShouldSuffocate(pos.x + 1, pos.y, pos.z) &&
                BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x + 1, pos.y + 1, pos.z, -1))
                connectsEast = true;
            if (ctx.BlockReader.ShouldSuffocate(pos.x, pos.y, pos.z - 1) &&
                BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x, pos.y + 1, pos.z - 1, -1))
                connectsNorth = true;
            if (ctx.BlockReader.ShouldSuffocate(pos.x, pos.y, pos.z + 1) &&
                BlockRedstoneWire.isPowerProviderOrWire(ctx.BlockReader, pos.x, pos.y + 1, pos.z + 1, -1))
                connectsSouth = true;
        }

        // --- 4. Determine Shape ---
        float renderMinX = pos.x, renderMaxX = pos.x + 1;
        float renderMinZ = pos.z, renderMaxZ = pos.z + 1;
        int shapeType = 0; // 0 = Cross, 1 = East/West, 2 = North/South

        if ((connectsWest || connectsEast) && !connectsNorth && !connectsSouth) shapeType = 1;
        if ((connectsNorth || connectsSouth) && !connectsEast && !connectsWest) shapeType = 2;

        if (shapeType != 0) // Use the "Straight Line" texture variant
        {
            minU = (texU + 16) / 256.0F;
            maxU = (texU + 16 + 15.99F) / 256.0F;
        }

        if (shapeType == 0)
        {
            if (connectsWest || connectsEast || connectsNorth || connectsSouth)
            {
                if (!connectsWest)
                {
                    renderMinX += 0.3125F;
                    minU += 0.01953125F;
                }

                if (!connectsEast)
                {
                    renderMaxX -= 0.3125F;
                    maxU -= 0.01953125F;
                }

                if (!connectsNorth)
                {
                    renderMinZ += 0.3125F;
                    minV += 0.01953125F;
                }

                if (!connectsSouth)
                {
                    renderMaxZ -= 0.3125F;
                    maxV -= 0.01953125F;
                }
            }
        }

        // --- 5. Render Horizontal Ground Quad ---
        float groundY = pos.y + 0.015625F;

        // Handle UV Rotation for North/South (Shape 2)
        float u1 = minU, u2 = maxU, u3 = maxU, u4 = minU;
        float v1 = minV, v2 = minV, v3 = maxV, v4 = maxV;

        if (shapeType == 2)
        {
            u1 = maxU;
            u2 = maxU;
            u3 = minU;
            u4 = minU;
            v1 = maxV;
            v2 = minV;
            v3 = minV;
            v4 = maxV;
        }

        // Main Wire
        ctx.Tess.addVertexWithUV(renderMaxX, groundY, renderMaxZ, u3, v3);
        ctx.Tess.addVertexWithUV(renderMaxX, groundY, renderMinZ, u2, v2);
        ctx.Tess.addVertexWithUV(renderMinX, groundY, renderMinZ, u1, v1);
        ctx.Tess.addVertexWithUV(renderMinX, groundY, renderMaxZ, u4, v4);

        // Shadow Shroud
        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
        float shroudVOffset = 1.0F / 16.0F;
        ctx.Tess.addVertexWithUV(renderMaxX, groundY, renderMaxZ, u3, v3 + shroudVOffset);
        ctx.Tess.addVertexWithUV(renderMaxX, groundY, renderMinZ, u2, v2 + shroudVOffset);
        ctx.Tess.addVertexWithUV(renderMinX, groundY, renderMinZ, u1, v1 + shroudVOffset);
        ctx.Tess.addVertexWithUV(renderMinX, groundY, renderMaxZ, u4, v4 + shroudVOffset);

        // --- 6. Render Slopes ---
        if (!ctx.BlockReader.ShouldSuffocate(pos.x, pos.y + 1, pos.z))
        {
            // Reset to the straight texture variant for slopes
            minU = (texU + 16) / 256.0F;
            maxU = (texU + 16 + 15.99F) / 256.0F;
            float slopeHeight = pos.y + 1.021875F;

            // West Slope
            if (ctx.BlockReader.ShouldSuffocate(pos.x - 1, pos.y, pos.z) &&
                ctx.BlockReader.GetBlockId(pos.x - 1, pos.y + 1, pos.z) == block.id)
            {
                ctx.Tess.setColorOpaque_F(luminance * r, luminance * g, luminance * b);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, slopeHeight, pos.z + 1, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, pos.y, pos.z + 1, minU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, pos.y, pos.z + 0, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, slopeHeight, pos.z + 0, maxU, maxV);

                ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, slopeHeight, pos.z + 1, maxU, minV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, pos.y, pos.z + 1, minU, minV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, pos.y, pos.z + 0, minU, maxV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0.015625f, slopeHeight, pos.z + 0, maxU, maxV + shroudVOffset);
            }

            // East Slope
            if (ctx.BlockReader.ShouldSuffocate(pos.x + 1, pos.y, pos.z) &&
                ctx.BlockReader.GetBlockId(pos.x + 1, pos.y + 1, pos.z) == block.id)
            {
                ctx.Tess.setColorOpaque_F(luminance * r, luminance * g, luminance * b);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, pos.y, pos.z + 1, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, slopeHeight, pos.z + 1, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, slopeHeight, pos.z + 0, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, pos.y, pos.z + 0, minU, minV);

                ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, pos.y, pos.z + 1, minU, maxV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, slopeHeight, pos.z + 1, maxU, maxV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, slopeHeight, pos.z + 0, maxU, minV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 1 - 0.015625f, pos.y, pos.z + 0, minU, minV + shroudVOffset);
            }

            // North Slope
            if (ctx.BlockReader.ShouldSuffocate(pos.x, pos.y, pos.z - 1) &&
                ctx.BlockReader.GetBlockId(pos.x, pos.y + 1, pos.z - 1) == block.id)
            {
                ctx.Tess.setColorOpaque_F(luminance * r, luminance * g, luminance * b);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, pos.z + 0.015625f, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 1, slopeHeight, pos.z + 0.015625f, maxU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 0, slopeHeight, pos.z + 0.015625f, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 0, pos.y, pos.z + 0.015625f, minU, minV);

                ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, pos.z + 0.015625f, minU, maxV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 1, slopeHeight, pos.z + 0.015625f, maxU, maxV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0, slopeHeight, pos.z + 0.015625f, maxU, minV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0, pos.y, pos.z + 0.015625f, minU, minV + shroudVOffset);
            }

            // South Slope
            if (ctx.BlockReader.ShouldSuffocate(pos.x, pos.y, pos.z + 1) &&
                ctx.BlockReader.GetBlockId(pos.x, pos.y + 1, pos.z + 1) == block.id)
            {
                ctx.Tess.setColorOpaque_F(luminance * r, luminance * g, luminance * b);
                ctx.Tess.addVertexWithUV(pos.x + 1, slopeHeight, pos.z + 1 - 0.015625f, maxU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, pos.z + 1 - 0.015625f, minU, minV);
                ctx.Tess.addVertexWithUV(pos.x + 0, pos.y, pos.z + 1 - 0.015625f, minU, maxV);
                ctx.Tess.addVertexWithUV(pos.x + 0, slopeHeight, pos.z + 1 - 0.015625f, maxU, maxV);

                ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);
                ctx.Tess.addVertexWithUV(pos.x + 1, slopeHeight, pos.z + 1 - 0.015625f, maxU, minV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 1, pos.y, pos.z + 1 - 0.015625f, minU, minV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0, pos.y, pos.z + 1 - 0.015625f, minU, maxV + shroudVOffset);
                ctx.Tess.addVertexWithUV(pos.x + 0, slopeHeight, pos.z + 1 - 0.015625f, maxU, maxV + shroudVOffset);
            }
        }

        return true;
    }
}
