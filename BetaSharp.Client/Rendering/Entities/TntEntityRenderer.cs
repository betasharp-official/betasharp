using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Rendering.Entities;

public class TntEntityRenderer : EntityRenderer
{
    public TntEntityRenderer()
    {
        ShadowRadius = 0.5F;
    }

    public void render(EntityTNTPrimed var1, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        float var10;
        if (var1.fuse - tickDelta + 1.0F < 10.0F)
        {
            var10 = 1.0F - (var1.fuse - tickDelta + 1.0F) / 10.0F;
            if (var10 < 0.0F)
            {
                var10 = 0.0F;
            }

            if (var10 > 1.0F)
            {
                var10 = 1.0F;
            }

            var10 *= var10;
            var10 *= var10;
            float var11 = 1.0F + var10 * 0.3F;
            GLManager.GL.Scale(var11, var11, var11);
        }

        var10 = (1.0F - (var1.fuse - tickDelta + 1.0F) / 100.0F) * 0.8F;
        loadTexture("/terrain.png");
        BlockRenderer.RenderBlockOnInventory(Block.TNT, 0, var1.getBrightnessAtEyes(tickDelta), Tessellator.instance);
        if (var1.fuse / 5 % 2 == 0)
        {
            GLManager.GL.Disable(GLEnum.Texture2D);
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.DstAlpha);
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, var10);
            BlockRenderer.RenderBlockOnInventory(Block.TNT, 0, 1.0F, Tessellator.instance);
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.Texture2D);
        }
        GLManager.GL.PopMatrix();
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityTNTPrimed)target, x, y, z, yaw, tickDelta);
    }
}
