using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class ReedRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        float luminance = block.getLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
        int colorMultiplier = block.getColorMultiplier(ctx.BlockReader, pos.x, pos.y, pos.z);
        float r = (colorMultiplier >> 16 & 255) / 255.0F;
        float g = (colorMultiplier >> 8 & 255) / 255.0F;
        float b = (colorMultiplier & 255) / 255.0F;

        ctx.Tess.setColorOpaque_F(luminance * r, luminance * g, luminance * b);

        float renderX = pos.x;
        float renderY = pos.y;
        float renderZ = pos.z;

        // Apply random organic offset for grass so it doesn't look grid-aligned
        if (block == Block.Grass) // Assuming Block.TallGrass or equivalent
        {
            long hash = pos.x * 3129871L ^ pos.z * 116129781L ^ pos.y;
            hash = hash * hash * 42317861L + hash * 11L;

            renderX += (((hash >> 16 & 15L) / 15.0F) - 0.5F) * 0.5F;
            renderY += (((hash >> 20 & 15L) / 15.0F) - 1.0F) * 0.2F;
            renderZ += (((hash >> 24 & 15L) / 15.0F) - 0.5F) * 0.5F;
        }

        RenderCrossedSquares(block, ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z), renderX, renderY, renderZ, ref ctx);
        return true;
    }

    private void RenderCrossedSquares(Block block, int metadata, float x, float y, float z,
        ref BlockRenderContext ctx)
    {
        int textureId = block.GetTexture(0, metadata);
        if (ctx.OverrideTexture >= 0)
        {
            textureId = ctx.OverrideTexture;
        }

        // Convert texture ID to UV coordinates (0.0 to 1.0 range)
        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;
        float minU = texU / 256.0F;
        float maxU = (texU + 15.99F) / 256.0F;
        float minV = texV / 256.0F;
        float maxV = (texV + 15.99F) / 256.0F;

        // Magic number 0.45 means the planes stretch from 0.05 to 0.95 within the block.
        // This slight inset prevents Z-fighting (flickering) if the plant touches an adjacent solid block.
        float minOffset = 0.5F - 0.45F; // 0.05
        float maxOffset = 0.5F + 0.45F; // 0.95

        float minX = x + minOffset;
        float maxX = x + maxOffset;
        float minZ = z + minOffset;
        float maxZ = z + maxOffset;

        // --- First Diagonal Plane (Bottom-Left to Top-Right across the X/Z grid) ---

        // Front side
        ctx.Tess.addVertexWithUV(minX, y + 1.0f, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0f, minZ, minU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0f, maxZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 1.0f, maxZ, maxU, minV);

        // Back side (reversed winding order and UVs)
        ctx.Tess.addVertexWithUV(maxX, y + 1.0f, maxZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0f, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0f, minZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 1.0f, minZ, maxU, minV);

        // --- Second Diagonal Plane (Top-Left to Bottom-Right across the X/Z grid) ---

        // Front side
        ctx.Tess.addVertexWithUV(minX, y + 1.0f, maxZ, minU, minV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0f, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0f, minZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 1.0f, minZ, maxU, minV);

        // Back side (reversed winding order and UVs)
        ctx.Tess.addVertexWithUV(maxX, y + 1.0f, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0f, minZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0f, maxZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 1.0f, maxZ, maxU, minV);
    }
}
