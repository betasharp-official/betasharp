using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class PaintingEntityRenderer : EntityRenderer
{
    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        RenderPainting((EntityPainting)target, x, y, z, yaw);
    }

    private void RenderPainting(EntityPainting painting, double x, double y, double z, float yaw)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Rotate(yaw, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Enable(GLEnum.RescaleNormal);

        loadTexture("/art/kz.png");

        EnumArt art = painting.Art;
        float pixelScale = 1.0F / 16.0F;
        GLManager.GL.Scale(pixelScale, pixelScale, pixelScale);

        RenderPaintingQuads(painting, art.SizeX, art.SizeY, art.OffsetX, art.OffsetY);

        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }

    private void RenderPaintingQuads(EntityPainting painting, int width, int height, int textureX, int textureY)
    {
        float leftBound = -width / 2.0F;
        float bottomBound = -height / 2.0F;
        float frontZ = -0.5F;
        float backZ = 0.5F;

        for (int tileX = 0; tileX < width / 16; ++tileX)
        {
            for (int tileY = 0; tileY < height / 16; ++tileY)
            {
                float xMax = leftBound + (tileX + 1) * 16;
                float xMin = leftBound + tileX * 16;
                float yMax = bottomBound + (tileY + 1) * 16;
                float yMin = bottomBound + tileY * 16;

                UpdateLighting(painting, (xMax + xMin) / 2.0F, (yMax + yMin) / 2.0F);

                float uMax = (textureX + width - tileX * 16) / 256.0F;
                float uMin = (textureX + width - (tileX + 1) * 16) / 256.0F;
                float vMax = (textureY + height - tileY * 16) / 256.0F;
                float vMin = (textureY + height - (tileY + 1) * 16) / 256.0F;

                float edgeUMin = 12.0F / 16.0F;
                float edgeUMax = 13.0F / 16.0F;
                float edgeVMin = 0.0F;
                float edgeVMax = 1.0F / 16.0F;

                Tessellator tess = Tessellator.instance;
                tess.startDrawingQuads();

                // Front Face (The Art)
                tess.setNormal(0.0F, 0.0F, -1.0F);
                tess.addVertexWithUV(xMax, yMin, frontZ, uMin, vMax);
                tess.addVertexWithUV(xMin, yMin, frontZ, uMax, vMax);
                tess.addVertexWithUV(xMin, yMax, frontZ, uMax, vMin);
                tess.addVertexWithUV(xMax, yMax, frontZ, uMin, vMin);

                // Back Face (The Wood)
                tess.setNormal(0.0F, 0.0F, 1.0F);
                tess.addVertexWithUV(xMax, yMax, backZ, edgeUMin, edgeVMin);
                tess.addVertexWithUV(xMin, yMax, backZ, edgeUMax, edgeVMin);
                tess.addVertexWithUV(xMin, yMin, backZ, edgeUMax, edgeVMax);
                tess.addVertexWithUV(xMax, yMin, backZ, edgeUMin, edgeVMax);

                // Side/Top/Bottom Edges
                tess.setNormal(0.0F, -1.0F, 0.0F); // Top
                tess.addVertexWithUV(xMax, yMax, frontZ, edgeUMin, edgeVMin);
                tess.addVertexWithUV(xMin, yMax, frontZ, edgeUMax, edgeVMin);
                tess.addVertexWithUV(xMin, yMax, backZ, edgeUMax, edgeVMax);
                tess.addVertexWithUV(xMax, yMax, backZ, edgeUMin, edgeVMax);

                tess.setNormal(0.0F, 1.0F, 0.0F); // Bottom
                tess.addVertexWithUV(xMax, yMin, backZ, edgeUMin, edgeVMin);
                tess.addVertexWithUV(xMin, yMin, backZ, edgeUMax, edgeVMin);
                tess.addVertexWithUV(xMin, yMin, frontZ, edgeUMax, edgeVMax);
                tess.addVertexWithUV(xMax, yMin, frontZ, edgeUMin, edgeVMax);

                tess.setNormal(-1.0F, 0.0F, 0.0F); // Left
                tess.addVertexWithUV(xMax, yMax, backZ, edgeUMax, edgeVMin);
                tess.addVertexWithUV(xMax, yMin, backZ, edgeUMax, edgeVMax);
                tess.addVertexWithUV(xMax, yMin, frontZ, edgeUMin, edgeVMax);
                tess.addVertexWithUV(xMax, yMax, frontZ, edgeUMin, edgeVMin);

                tess.setNormal(1.0F, 0.0F, 0.0F); // Right
                tess.addVertexWithUV(xMin, yMax, frontZ, edgeUMax, edgeVMin);
                tess.addVertexWithUV(xMin, yMin, frontZ, edgeUMax, edgeVMax);
                tess.addVertexWithUV(xMin, yMin, backZ, edgeUMin, edgeVMax);
                tess.addVertexWithUV(xMin, yMax, backZ, edgeUMin, edgeVMin);

                tess.draw();
            }
        }
    }

    private void UpdateLighting(EntityPainting painting, float offsetX, float offsetY)
    {
        int checkX = MathHelper.Floor(painting.x);
        int checkY = MathHelper.Floor(painting.y + (offsetY / 16.0F));
        int checkZ = MathHelper.Floor(painting.z);

        // Offset the light check based on orientation to ensure we aren't sampling inside the wall
        switch (painting.Direction)
        {
            case 0: checkX = MathHelper.Floor(painting.x + (offsetX / 16.0F)); break;
            case 1: checkZ = MathHelper.Floor(painting.z - (offsetX / 16.0F)); break;
            case 2: checkX = MathHelper.Floor(painting.x - (offsetX / 16.0F)); break;
            case 3: checkZ = MathHelper.Floor(painting.z + (offsetX / 16.0F)); break;
        }

        float light = Dispatcher.world.getLuminance(checkX, checkY, checkZ);
        GLManager.GL.Color3(light, light, light);
    }


}
