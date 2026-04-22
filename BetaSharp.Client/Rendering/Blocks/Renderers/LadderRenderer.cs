using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class LadderRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        int textureId = block.GetTexture(0);
        if (ctx.OverrideTexture >= 0)
        {
            textureId = ctx.OverrideTexture;
        }

        float luminance = block.getLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
        ctx.Tess.setColorOpaque_F(luminance, luminance, luminance);

        int texU = (textureId & 15) << 4;
        int texV = textureId & 240;
        float minU = texU / 256.0f;
        float maxU = (texU + 15.99f) / 256.0f;
        float minV = texV / 256.0f;
        float maxV = (texV + 15.99f) / 256.0f;

        int metadata = ctx.BlockReader.GetBlockMeta(pos.x, pos.y, pos.z);

        // Push the ladder slightly off the wall
        float offset = 0.05f;

        if (metadata == 5)
        {
            ctx.Tess.addVertexWithUV(pos.x + offset, pos.y + 1.0D, pos.z + 1.0D, minU, minV);
            ctx.Tess.addVertexWithUV(pos.x + offset, pos.y + 0.0D, pos.z + 1.0D, minU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + offset, pos.y + 0.0D, pos.z + 0.0D, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + offset, pos.y + 1.0D, pos.z + 0.0D, maxU, minV);
        }
        else if (metadata == 4)
        {
            ctx.Tess.addVertexWithUV(pos.x + 1.0D - offset, pos.y + 0.0D, pos.z + 1.0D, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 1.0D - offset, pos.y + 1.0D, pos.z + 1.0D, maxU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 1.0D - offset, pos.y + 1.0D, pos.z + 0.0D, minU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 1.0D - offset, pos.y + 0.0D, pos.z + 0.0D, minU, maxV);
        }
        else if (metadata == 3)
        {
            ctx.Tess.addVertexWithUV(pos.x + 1.0D, pos.y + 0.0D, pos.z + offset, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 1.0D, pos.y + 1.0D, pos.z + offset, maxU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 0.0D, pos.y + 1.0D, pos.z + offset, minU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 0.0D, pos.y + 0.0D, pos.z + offset, minU, maxV);
        }
        else if (metadata == 2)
        {
            ctx.Tess.addVertexWithUV(pos.x + 1.0D, pos.y + 1.0D, pos.z + 1.0D - offset, minU, minV);
            ctx.Tess.addVertexWithUV(pos.x + 1.0D, pos.y + 0.0D, pos.z + 1.0D - offset, minU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 0.0D, pos.y + 0.0D, pos.z + 1.0D - offset, maxU, maxV);
            ctx.Tess.addVertexWithUV(pos.x + 0.0D, pos.y + 1.0D, pos.z + 1.0D - offset, maxU, minV);
        }

        return true;
    }
}
