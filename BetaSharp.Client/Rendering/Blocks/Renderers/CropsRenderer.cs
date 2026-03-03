using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class CropsRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        float luminance = block.getLuminance(ctx.World, pos.x, pos.y, pos.z);
        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);

        int metadata = ctx.World.getBlockMeta(pos.x, pos.y, pos.z);

        // Crops are pushed down slightly into the soil block
        float yOffset = pos.y - (1.0f / 16.0f);

        RenderCropQuads(block, metadata, pos.x, yOffset, pos.z, ref ctx);

        return true;
    }

    private void RenderCropQuads(Block block, int metadata, float x, float y, float z, ref BlockRenderContext ctx)
    {
        int textureId = block.getTexture(0, metadata);

        if (ctx.OverrideTexture >= 0)
        {
            textureId = ctx.OverrideTexture;
        }

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;
        float minU = texU / 256.0F;
        float maxU = (texU + 15.99F) / 256.0F;
        float minV = texV / 256.0F;
        float maxV = (texV + 15.99F) / 256.0F;

        float minX = x + 0.5f - 0.25f; // Left plane X
        float maxX = x + 0.5f + 0.25f; // Right plane X
        float minZ = z + 0.5f - 0.5f; // Front plane Z
        float maxZ = z + 0.5f + 0.5f; // Back plane Z

        // --- Vertical Planes (North-South aligned) ---
        ctx.Tess.addVertexWithUV(minX, y + 1.0D, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, minZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, maxZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 1.0D, maxZ, maxU, minV);

        ctx.Tess.addVertexWithUV(minX, y + 1.0D, maxZ, minU, minV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, minZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 1.0D, minZ, maxU, minV);

        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, maxZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, minZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, minZ, maxU, minV);

        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, minZ, minU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, maxZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, maxZ, maxU, minV);

        // --- Horizontal Planes (East-West aligned) ---
        // Reposition coordinates for the crossing planes
        minX = x + 0.5F - 0.5F;
        maxX = x + 0.5F + 0.5F;
        minZ = z + 0.5F - 0.25F;
        maxZ = z + 0.5F + 0.25F;

        ctx.Tess.addVertexWithUV(minX, y + 1.0D, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, minZ, minU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, minZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, minZ, maxU, minV);

        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, minZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, minZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 1.0D, minZ, maxU, minV);

        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, maxZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, maxZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(minX, y + 1.0D, maxZ, maxU, minV);

        ctx.Tess.addVertexWithUV(minX, y + 1.0D, maxZ, minU, minV);
        ctx.Tess.addVertexWithUV(minX, y + 0.0D, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 0.0D, maxZ, maxU, maxV);
        ctx.Tess.addVertexWithUV(maxX, y + 1.0D, maxZ, maxU, minV);
    }
}
