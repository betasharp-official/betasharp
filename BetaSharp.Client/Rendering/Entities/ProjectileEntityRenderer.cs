using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class ProjectileEntityRenderer : EntityRenderer
{

    private readonly int itemIconIndex;

    public ProjectileEntityRenderer(int var1)
    {
        itemIconIndex = var1;
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
        loadTexture("/gui/items.png");
        Tessellator var10 = Tessellator.instance;
        float var11 = (itemIconIndex % 16 * 16 + 0) / 256.0F;
        float var12 = (itemIconIndex % 16 * 16 + 16) / 256.0F;
        float var13 = (itemIconIndex / 16 * 16 + 0) / 256.0F;
        float var14 = (itemIconIndex / 16 * 16 + 16) / 256.0F;
        float var15 = 1.0F;
        float var16 = 0.5F;
        float var17 = 0.25F;
        GLManager.GL.Rotate(180.0F - Dispatcher.playerViewY, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-Dispatcher.playerViewX, 1.0F, 0.0F, 0.0F);
        var10.startDrawingQuads();
        var10.setNormal(0.0F, 1.0F, 0.0F);
        var10.addVertexWithUV((double)(0.0F - var16), (double)(0.0F - var17), 0.0D, (double)var11, (double)var14);
        var10.addVertexWithUV((double)(var15 - var16), (double)(0.0F - var17), 0.0D, (double)var12, (double)var14);
        var10.addVertexWithUV((double)(var15 - var16), (double)(1.0F - var17), 0.0D, (double)var12, (double)var13);
        var10.addVertexWithUV((double)(0.0F - var16), (double)(1.0F - var17), 0.0D, (double)var11, (double)var13);
        var10.draw();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }
}