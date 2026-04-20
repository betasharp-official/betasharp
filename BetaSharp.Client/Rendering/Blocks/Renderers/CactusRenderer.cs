using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class CactusRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        Box bounds = ctx.OverrideBounds ?? block.BoundingBox;
        bool hasRendered = false;

        // Force the helper to use flat shading so it doesn't override our colors with the dummy struct
        var flatCtx = ctx with
        {
            EnableAo = false
        };

        // 1. Calculate the specific biome/tint color for this cactus
        int colorMultiplier = block.GetColorMultiplier(ctx.BlockReader, pos.x, pos.y, pos.z);
        float red = (colorMultiplier >> 16 & 255) / 255.0F;
        float green = (colorMultiplier >> 8 & 255) / 255.0F;
        float blue = (colorMultiplier & 255) / 255.0F;

        // 2. Base directional lighting multipliers
        const float lightBottom = 0.5F;
        const float lightTop = 1.0F;
        const float lightZ = 0.8F; // East/West faces
        const float lightX = 0.6F; // North/South faces

        // Pre-calculate tinted colors for each face
        float rBottom = lightBottom * red, gBottom = lightBottom * green, bBottom = lightBottom * blue;
        float rTop = lightTop * red, gTop = lightTop * green, bTop = lightTop * blue;
        float rZ = lightZ * red, gZ = lightZ * green, bZ = lightZ * blue;
        float rX = lightX * red, gX = lightX * green, bX = lightX * blue;

        // 1/16th of a block = exactly 1 pixel width in a standard 16x16 texture
        float inset = 1.0F / 16.0F;

        float centerLuminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y, pos.z);
        float faceLuminance;

        FaceColors dummyColors = new();

        // --- Bottom Face (Y - 1) ---
        if (flatCtx.RenderAllFaces || bounds.MinY > 0.0D || block.IsSideVisible(ctx.BlockReader, pos.x, pos.y - 1, pos.z, 0))
        {
            faceLuminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y - 1, pos.z);
            ctx.Tess.setColorOpaque_F(rBottom * faceLuminance, gBottom * faceLuminance, bBottom * faceLuminance);

            int tex = block.GetTextureId(ctx.BlockReader, pos.x, pos.y, pos.z, 0);
            flatCtx.DrawBottomFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);
            hasRendered = true;
        }

        // --- Top Face (Y + 1) ---
        if (flatCtx.RenderAllFaces || bounds.MaxY < 1.0D || block.IsSideVisible(ctx.BlockReader, pos.x, pos.y + 1, pos.z, Side.Up))
        {
            faceLuminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y + 1, pos.z);
            if (Math.Abs(bounds.MaxY - 1.0D) > 0.1 && !block.Material.IsFluid)
            {
                faceLuminance = centerLuminance;
            }

            ctx.Tess.setColorOpaque_F(rTop * faceLuminance, gTop * faceLuminance, bTop * faceLuminance);

            int tex = block.GetTextureId(ctx.BlockReader, pos.x, pos.y, pos.z, Side.Up);
            flatCtx.DrawTopFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);
            hasRendered = true;
        }

        // --- East Face (Z - 1) ---
        if (flatCtx.RenderAllFaces || bounds.MinZ > 0.0D || block.IsSideVisible(ctx.BlockReader, pos.x, pos.y, pos.z - 1, Side.North))
        {
            faceLuminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y, pos.z - 1);
            if (bounds.MinZ > 0.0D) faceLuminance = centerLuminance;

            ctx.Tess.setColorOpaque_F(rZ * faceLuminance, gZ * faceLuminance, bZ * faceLuminance);

            ctx.Tess.setTranslationF(0.0F, 0.0F, inset);

            int tex = block.GetTextureId(ctx.BlockReader, pos.x, pos.y, pos.z, Side.North);
            flatCtx.DrawEastFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);

            ctx.Tess.setTranslationF(0.0F, 0.0F, -inset);
            hasRendered = true;
        }

        // --- West Face (Z + 1) ---
        if (flatCtx.RenderAllFaces || bounds.MaxZ < 1.0D || block.IsSideVisible(ctx.BlockReader, pos.x, pos.y, pos.z + 1, Side.South))
        {
            faceLuminance = block.GetLuminance(ctx.Lighting, pos.x, pos.y, pos.z + 1);
            if (bounds.MaxZ < 1.0D) faceLuminance = centerLuminance;

            ctx.Tess.setColorOpaque_F(rZ * faceLuminance, gZ * faceLuminance, bZ * faceLuminance);

            ctx.Tess.setTranslationF(0.0F, 0.0F, -inset);

            int tex = block.GetTextureId(ctx.BlockReader, pos.x, pos.y, pos.z, Side.South);
            flatCtx.DrawWestFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);

            ctx.Tess.setTranslationF(0.0F, 0.0F, inset);
            hasRendered = true;
        }

        // --- North Face (X - 1) ---
        if (flatCtx.RenderAllFaces || bounds.MinX > 0.0D || block.IsSideVisible(ctx.BlockReader, pos.x - 1, pos.y, pos.z, Side.West))
        {
            faceLuminance = block.GetLuminance(ctx.Lighting, pos.x - 1, pos.y, pos.z);
            if (bounds.MinX > 0.0D) faceLuminance = centerLuminance;

            ctx.Tess.setColorOpaque_F(rX * faceLuminance, gX * faceLuminance, bX * faceLuminance);

            ctx.Tess.setTranslationF(inset, 0.0F, 0.0F);

            int tex = block.GetTextureId(ctx.BlockReader, pos.x, pos.y, pos.z, Side.West);
            flatCtx.DrawNorthFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);

            ctx.Tess.setTranslationF(-inset, 0.0F, 0.0F);
            hasRendered = true;
        }

        // --- South Face (X + 1) ---
        if (flatCtx.RenderAllFaces || bounds.MaxX < 1.0D || block.IsSideVisible(ctx.BlockReader, pos.x + 1, pos.y, pos.z, Side.East))
        {
            faceLuminance = block.GetLuminance(ctx.Lighting, pos.x + 1, pos.y, pos.z);
            if (bounds.MaxX < 1.0D) faceLuminance = centerLuminance;

            ctx.Tess.setColorOpaque_F(rX * faceLuminance, gX * faceLuminance, bX * faceLuminance);

            ctx.Tess.setTranslationF(-inset, 0.0F, 0.0F);

            int tex = block.GetTextureId(ctx.BlockReader, pos.x, pos.y, pos.z, Side.East);
            flatCtx.DrawSouthFace(block, new Vec3D(pos.x, pos.y, pos.z), dummyColors, tex);

            ctx.Tess.setTranslationF(inset, 0.0F, 0.0F);
            hasRendered = true;
        }

        return hasRendered;
    }
}
