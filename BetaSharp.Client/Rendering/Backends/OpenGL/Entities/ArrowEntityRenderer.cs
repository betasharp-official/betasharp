using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Legacy;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class ArrowEntityRenderer : EntityRenderer
{
    public void renderArrow(EntityArrow var1, double var2, double var4, double var6, float var8, float var9)
    {
        if (var1.prevYaw != 0.0F || var1.prevPitch != 0.0F)
        {
            loadTexture("/item/arrows.png");
            Scene.PushMatrix();
            Scene.Translate((float)var2, (float)var4, (float)var6);
            Scene.Rotate(var1.prevYaw + (var1.yaw - var1.prevYaw) * var9 - 90.0F, 0.0F, 1.0F, 0.0F);
            Scene.Rotate(var1.prevPitch + (var1.pitch - var1.prevPitch) * var9, 0.0F, 0.0F, 1.0F);
            Tessellator var10 = Tessellator.instance;
            byte var11 = 0;
            float var12 = 0.0F;
            float var13 = 0.5F;
            float var14 = (0 + var11 * 10) / 32.0F;
            float var15 = (5 + var11 * 10) / 32.0F;
            float var16 = 0.0F;
            float var17 = 0.15625F;
            float var18 = (5 + var11 * 10) / 32.0F;
            float var19 = (10 + var11 * 10) / 32.0F;
            float var20 = 0.05625F;
            Scene.Enable(SceneRenderCapability.RescaleNormal);
            float var21 = var1.arrowShake - var9;
            if (var21 > 0.0F)
            {
                float var22 = -MathHelper.Sin(var21 * 3.0F) * var21;
                Scene.Rotate(var22, 0.0F, 0.0F, 1.0F);
            }

            Scene.Rotate(45.0F, 1.0F, 0.0F, 0.0F);
            Scene.Scale(var20, var20, var20);
            Scene.Translate(-4.0F, 0.0F, 0.0F);
            Scene.SetNormal(var20, 0.0F, 0.0F);
            var10.startDrawingQuads();
            var10.addVertexWithUV(-7.0D, -2.0D, -2.0D, (double)var16, (double)var18);
            var10.addVertexWithUV(-7.0D, -2.0D, 2.0D, (double)var17, (double)var18);
            var10.addVertexWithUV(-7.0D, 2.0D, 2.0D, (double)var17, (double)var19);
            var10.addVertexWithUV(-7.0D, 2.0D, -2.0D, (double)var16, (double)var19);
            var10.draw();
            Scene.SetNormal(-var20, 0.0F, 0.0F);
            var10.startDrawingQuads();
            var10.addVertexWithUV(-7.0D, 2.0D, -2.0D, (double)var16, (double)var18);
            var10.addVertexWithUV(-7.0D, 2.0D, 2.0D, (double)var17, (double)var18);
            var10.addVertexWithUV(-7.0D, -2.0D, 2.0D, (double)var17, (double)var19);
            var10.addVertexWithUV(-7.0D, -2.0D, -2.0D, (double)var16, (double)var19);
            var10.draw();

            for (int var23 = 0; var23 < 4; ++var23)
            {
                Scene.Rotate(90.0F, 1.0F, 0.0F, 0.0F);
                Scene.SetNormal(0.0F, 0.0F, var20);
                var10.startDrawingQuads();
                var10.addVertexWithUV(-8.0D, -2.0D, 0.0D, (double)var12, (double)var14);
                var10.addVertexWithUV(8.0D, -2.0D, 0.0D, (double)var13, (double)var14);
                var10.addVertexWithUV(8.0D, 2.0D, 0.0D, (double)var13, (double)var15);
                var10.addVertexWithUV(-8.0D, 2.0D, 0.0D, (double)var12, (double)var15);
                var10.draw();
            }

            Scene.Disable(SceneRenderCapability.RescaleNormal);
            Scene.PopMatrix();
        }
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderArrow((EntityArrow)target, x, y, z, yaw, tickDelta);
    }
}
