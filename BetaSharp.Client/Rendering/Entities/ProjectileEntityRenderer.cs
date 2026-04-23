using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class ProjectileEntityRenderer : EntityRenderer
{

    private readonly int itemIconIndex;

    public ProjectileEntityRenderer(int itemIconIndex)
    {
        this.itemIconIndex = itemIconIndex;
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        GLManager.GL.Scale(0.5F, 0.5F, 0.5F);
        loadTexture("/gui/items.png");
        Tessellator tessellator = Tessellator.instance;
        float minU = (itemIconIndex % 16 * 16 + 0) / 256.0F;
        float maxU = (itemIconIndex % 16 * 16 + 16) / 256.0F;
        float minV = (itemIconIndex / 16 * 16 + 0) / 256.0F;
        float maxV = (itemIconIndex / 16 * 16 + 16) / 256.0F;
        float quadWidth = 1.0F;
        float xOffset = 0.5F;
        float yOffset = 0.25F;
        GLManager.GL.Rotate(180.0F - Dispatcher.PlayerViewY, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-Dispatcher.PlayerViewX, 1.0F, 0.0F, 0.0F);
        tessellator.startDrawingQuads();
        tessellator.setNormal(0.0F, 1.0F, 0.0F);
        tessellator.addVertexWithUV((double)(0.0F - xOffset), (double)(0.0F - yOffset), 0.0D, (double)minU, (double)maxV);
        tessellator.addVertexWithUV((double)(quadWidth - xOffset), (double)(0.0F - yOffset), 0.0D, (double)maxU, (double)maxV);
        tessellator.addVertexWithUV((double)(quadWidth - xOffset), (double)(1.0F - yOffset), 0.0D, (double)maxU, (double)minV);
        tessellator.addVertexWithUV((double)(0.0F - xOffset), (double)(1.0F - yOffset), 0.0D, (double)minU, (double)minV);
        tessellator.draw();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }
}
