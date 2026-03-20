using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class ArrowEntityRenderer : EntityRenderer
{

    public void renderArrow(EntityArrow arrow, double x, double y, double z, float yaw, float partialTicks)
    {
        if (arrow.prevYaw != 0.0F || arrow.prevPitch != 0.0F)
        {
            loadTexture("/item/arrows.png");
            GLManager.GL.PushMatrix();
            GLManager.GL.Translate((float)x, (float)y, (float)z);
            GLManager.GL.Rotate(arrow.prevYaw + (arrow.yaw - arrow.prevYaw) * partialTicks - 90.0F, 0.0F, 1.0F, 0.0F);
            GLManager.GL.Rotate(arrow.prevPitch + (arrow.pitch - arrow.prevPitch) * partialTicks, 0.0F, 0.0F, 1.0F);
            Tessellator tessellator = Tessellator.instance;
            byte arrowVariant = 0;
            float shaftUMin = 0.0F;
            float shaftUMax = 0.5F;
            float shaftVMin = (0 + arrowVariant * 10) / 32.0F;
            float shaftVMax = (5 + arrowVariant * 10) / 32.0F;
            float headUMin = 0.0F;
            float headUMax = 0.15625F;
            float headVMin = (5 + arrowVariant * 10) / 32.0F;
            float headVMax = (10 + arrowVariant * 10) / 32.0F;
            float scale = 0.05625F;
            GLManager.GL.Enable(GLEnum.RescaleNormal);
            float shakeRemaining = arrow.arrowShake - partialTicks;
            if (shakeRemaining > 0.0F)
            {
                float shakeAngle = -MathHelper.Sin(shakeRemaining * 3.0F) * shakeRemaining;
                GLManager.GL.Rotate(shakeAngle, 0.0F, 0.0F, 1.0F);
            }

            GLManager.GL.Rotate(45.0F, 1.0F, 0.0F, 0.0F);
            GLManager.GL.Scale(scale, scale, scale);
            GLManager.GL.Translate(-4.0F, 0.0F, 0.0F);
            GLManager.GL.Normal3(scale, 0.0F, 0.0F);
            tessellator.startDrawingQuads();
            tessellator.addVertexWithUV(-7.0D, -2.0D, -2.0D, (double)headUMin, (double)headVMin);
            tessellator.addVertexWithUV(-7.0D, -2.0D, 2.0D, (double)headUMax, (double)headVMin);
            tessellator.addVertexWithUV(-7.0D, 2.0D, 2.0D, (double)headUMax, (double)headVMax);
            tessellator.addVertexWithUV(-7.0D, 2.0D, -2.0D, (double)headUMin, (double)headVMax);
            tessellator.draw();
            GLManager.GL.Normal3(-scale, 0.0F, 0.0F);
            tessellator.startDrawingQuads();
            tessellator.addVertexWithUV(-7.0D, 2.0D, -2.0D, (double)headUMin, (double)headVMin);
            tessellator.addVertexWithUV(-7.0D, 2.0D, 2.0D, (double)headUMax, (double)headVMin);
            tessellator.addVertexWithUV(-7.0D, -2.0D, 2.0D, (double)headUMax, (double)headVMax);
            tessellator.addVertexWithUV(-7.0D, -2.0D, -2.0D, (double)headUMin, (double)headVMax);
            tessellator.draw();

            for (int i = 0; i < 4; ++i)
            {
                GLManager.GL.Rotate(90.0F, 1.0F, 0.0F, 0.0F);
                GLManager.GL.Normal3(0.0F, 0.0F, scale);
                tessellator.startDrawingQuads();
                tessellator.addVertexWithUV(-8.0D, -2.0D, 0.0D, (double)shaftUMin, (double)shaftVMin);
                tessellator.addVertexWithUV(8.0D, -2.0D, 0.0D, (double)shaftUMax, (double)shaftVMin);
                tessellator.addVertexWithUV(8.0D, 2.0D, 0.0D, (double)shaftUMax, (double)shaftVMax);
                tessellator.addVertexWithUV(-8.0D, 2.0D, 0.0D, (double)shaftUMin, (double)shaftVMax);
                tessellator.draw();
            }

            GLManager.GL.Disable(GLEnum.RescaleNormal);
            GLManager.GL.PopMatrix();
        }
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderArrow((EntityArrow)target, x, y, z, yaw, tickDelta);
    }
}