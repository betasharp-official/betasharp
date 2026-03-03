using BetaSharp.Blocks;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Blocks.Renderers;

public class BedRenderer : IBlockRenderer
{
    public bool Draw(Block block, in BlockPos pos, ref BlockRenderContext ctx)
    {
        Box bounds = ctx.OverrideBounds ?? block.BoundingBox;
        int metadata = ctx.World.getBlockMeta(pos.x, pos.y, pos.z);
        int direction = BlockBed.getDirection(metadata);
        bool isHead = BlockBed.isHeadOfBed(metadata);

        float lightBottom = 0.5F;
        float lightTop = 1.0F;
        float lightZ = 0.8F;
        float lightX = 0.6F;

        float centerLuminance = block.getLuminance(ctx.World, pos.x, pos.y, pos.z);

        // BOTTOM FACE
        ctx.Tess.setColorOpaque_F(lightBottom * centerLuminance, lightBottom * centerLuminance,
            lightBottom * centerLuminance);

        int texBottom = block.getTextureId(ctx.World, pos.x, pos.y, pos.z, 0);
        int texU = (texBottom & 15) << 4;
        int texV = texBottom & 240;

        float minU = texU / 256.0F;
        float maxU = (texU + 15.99f) / 256.0f;
        float minV = texV / 256.0F;
        float maxV = (texV + 15.99f) / 256.0f;

        float minX = (float)(pos.x + bounds.MinX);
        float maxX = (float)(pos.x + bounds.MaxX);
        float bedBottomY =(float)(pos.y + bounds.MinY + 0.1875f); // Bed legs are 3 pixels tall (3/16 = 0.1875)
        float minZ = (float)(pos.z + bounds.MinZ);
        float maxZ = (float)(pos.z + bounds.MaxZ);

        ctx.Tess.addVertexWithUV(minX, bedBottomY, maxZ, minU, maxV);
        ctx.Tess.addVertexWithUV(minX, bedBottomY, minZ, minU, minV);
        ctx.Tess.addVertexWithUV(maxX, bedBottomY, minZ, maxU, minV);
        ctx.Tess.addVertexWithUV(maxX, bedBottomY, maxZ, maxU, maxV);

        // TOP FACE
        float topLuminance = block.getLuminance(ctx.World, pos.x, pos.y + 1, pos.z);
        ctx.Tess.setColorOpaque_F(lightTop * topLuminance, lightTop * topLuminance, lightTop * topLuminance);

        int texTop = block.getTextureId(ctx.World, pos.x, pos.y, pos.z, 1);
        texU = (texTop & 15) << 4;
        texV = texTop & 240;

        minU = texU / 256.0F;
        maxU = (texU + 15.99f) / 256.0f;
        minV = texV / 256.0F;
        maxV = (texV + 15.99f) / 256.0f;

        float u1 = minU, u2 = maxU, u3 = minU, u4 = maxU;
        float v1 = minV, v2 = minV, v3 = maxV, v4 = maxV;

        // Rotate top texture based on bed orientation
        if (direction == 0) // South
        {
            u2 = minU;
            v2 = maxV;
            u3 = maxU;
            v3 = minV;
        }
        else if (direction == 2) // North
        {
            u1 = maxU;
            v1 = maxV;
            u4 = minU;
            v4 = minV;
        }
        else if (direction == 3) // East
        {
            u1 = maxU;
            v1 = maxV;
            u4 = minU;
            v4 = minV;
            u2 = minU;
            v2 = maxV;
            u3 = maxU;
            v3 = minV;
        }

        float bedTopY = (float)(pos.y + bounds.MaxY);

        ctx.Tess.addVertexWithUV(maxX, bedTopY, maxZ, u3, v3);
        ctx.Tess.addVertexWithUV(maxX, bedTopY, minZ, u1, v1);
        ctx.Tess.addVertexWithUV(minX, bedTopY, minZ, u2, v2);
        ctx.Tess.addVertexWithUV(minX, bedTopY, maxZ, u4, v4);

        // SIDE FACES
        int forwardDir = Facings.TO_DIR[direction];
        if (isHead)
        {
            forwardDir = Facings.TO_DIR[Facings.OPPOSITE[direction]];
        }

        byte textureFlipDir = 4;
        switch (direction)
        {
            case 0: textureFlipDir = 5; break;
            case 1:
                textureFlipDir = 3;
                goto case 2;
            case 2:
            default: break;
            case 3: textureFlipDir = 2; break;
        }

        float faceLuminance;
        var flatCtx = ctx with { EnableAo = false };
        // East Face (Z - 1)
        if (forwardDir != 2 && (ctx.RenderAllFaces || block.isSideVisible(ctx.World, pos.x, pos.y, pos.z - 1, 2)))
        {
            faceLuminance = bounds.MinZ > 0.0f
                ? centerLuminance
                : block.getLuminance(ctx.World, pos.x, pos.y, pos.z - 1);
            ctx.Tess.setColorOpaque_F(lightZ * faceLuminance, lightZ * faceLuminance, lightZ * faceLuminance);

            flatCtx.FlipTexture = textureFlipDir == 2;
            flatCtx.DrawEastFace(block, new Vec3D(pos.x, pos.y, pos.z), new FaceColors(),
                block.getTextureId(ctx.World, pos.x, pos.y, pos.z, 2));
        }

        // West Face (Z + 1)
        if (forwardDir != 3 && (ctx.RenderAllFaces || block.isSideVisible(ctx.World, pos.x, pos.y, pos.z + 1, 3)))
        {
            faceLuminance = bounds.MaxZ < 1.0f
                ? centerLuminance
                : block.getLuminance(ctx.World, pos.x, pos.y, pos.z + 1);
            ctx.Tess.setColorOpaque_F(lightZ * faceLuminance, lightZ * faceLuminance, lightZ * faceLuminance);

            flatCtx.FlipTexture = textureFlipDir == 3;
            flatCtx.DrawWestFace(block, new Vec3D(pos.x, pos.y, pos.z), new FaceColors(),
                block.getTextureId(ctx.World, pos.x, pos.y, pos.z, 3));
        }

        // North Face (X - 1)
        if (forwardDir != 4 && (ctx.RenderAllFaces || block.isSideVisible(ctx.World, pos.x - 1, pos.y, pos.z, 4)))
        {
            faceLuminance = bounds.MinX > 0.0f
                ? centerLuminance
                : block.getLuminance(ctx.World, pos.x - 1, pos.y, pos.z);
            ctx.Tess.setColorOpaque_F(lightX * faceLuminance, lightX * faceLuminance, lightX * faceLuminance);

            flatCtx.FlipTexture = textureFlipDir == 4;
            flatCtx.DrawNorthFace(block, new Vec3D(pos.x, pos.y, pos.z), new FaceColors(),
                block.getTextureId(ctx.World, pos.x, pos.y, pos.z, 4));
        }

        // South Face (X + 1)
        if (forwardDir != 5 && (ctx.RenderAllFaces || block.isSideVisible(ctx.World, pos.x + 1, pos.y, pos.z, 5)))
        {
            faceLuminance = bounds.MaxX < 1.0f
                ? centerLuminance
                : block.getLuminance(ctx.World, pos.x + 1, pos.y, pos.z);
            ctx.Tess.setColorOpaque_F(lightX * faceLuminance, lightX * faceLuminance, lightX * faceLuminance);

            flatCtx.FlipTexture = textureFlipDir == 5;
            flatCtx.DrawSouthFace(block, new Vec3D(pos.x, pos.y, pos.z), new FaceColors(),
                block.getTextureId(ctx.World, pos.x, pos.y, pos.z, 5));
        }

        return true;
    }
}
