using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;
using BetaSharp.Items;

namespace BetaSharp.Client.Rendering.Entities;

public class FireballEntityRenderer : EntityRenderer
{

    public void render(EntityFireball entity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Enable(GLEnum.RescaleNormal);
        float scale = 2.0F;
        GLManager.GL.Scale(scale / 1.0F, scale / 1.0F, scale / 1.0F);
        int textureId = Item.Snowball.getTextureId(0);
        loadTexture("/gui/items.png");
        Tessellator tessellator = Tessellator.instance;
        float uMin = (textureId % 16 * 16 + 0) / 256.0F;
        float uMax = (textureId % 16 * 16 + 16) / 256.0F;
        float vMin = (textureId / 16 * 16 + 0) / 256.0F;
        float vMax = (textureId / 16 * 16 + 16) / 256.0F;
        float width = 1.0F;
        float halfWidth = 0.5F;
        float quarterHeight = 0.25F;
        GLManager.GL.Rotate(180.0F - Dispatcher.playerViewY, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(-Dispatcher.playerViewX, 1.0F, 0.0F, 0.0F);
        tessellator.startDrawingQuads();
        tessellator.setNormal(0.0F, 1.0F, 0.0F);
        tessellator.addVertexWithUV((double)(0.0F - halfWidth), (double)(0.0F - quarterHeight), 0.0D, (double)uMin, (double)vMax);
        tessellator.addVertexWithUV((double)(width - halfWidth), (double)(0.0F - quarterHeight), 0.0D, (double)uMax, (double)vMax);
        tessellator.addVertexWithUV((double)(width - halfWidth), (double)(1.0F - quarterHeight), 0.0D, (double)uMax, (double)vMin);
        tessellator.addVertexWithUV((double)(0.0F - halfWidth), (double)(1.0F - quarterHeight), 0.0D, (double)uMin, (double)vMin);
        tessellator.draw();
        GLManager.GL.Disable(GLEnum.RescaleNormal);
        GLManager.GL.PopMatrix();
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityFireball)target, x, y, z, yaw, tickDelta);
    }
}