using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class TntEntityRenderer : EntityRenderer
{
    public TntEntityRenderer()
    {
        ShadowRadius = 0.5F;
    }

    public void render(EntityTntPrimed tntEntity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        float flashProgress;
        if (tntEntity.Fuse - tickDelta + 1.0F < 10.0F)
        {
            flashProgress = 1.0F - (tntEntity.Fuse - tickDelta + 1.0F) / 10.0F;
            if (flashProgress < 0.0F)
            {
                flashProgress = 0.0F;
            }

            if (flashProgress > 1.0F)
            {
                flashProgress = 1.0F;
            }

            flashProgress *= flashProgress;
            flashProgress *= flashProgress;
            float scale = 1.0F + flashProgress * 0.3F;
            GLManager.GL.Scale(scale, scale, scale);
        }

        flashProgress = (1.0F - (tntEntity.Fuse - tickDelta + 1.0F) / 100.0F) * 0.8F;
        loadTexture("/terrain.png");
        BlockRenderer.RenderBlockOnInventory(Block.TNT, 0, tntEntity.GetBrightnessAtEyes(tickDelta), Tessellator.instance);
        if (tntEntity.Fuse / 5 % 2 == 0)
        {
            GLManager.GL.Disable(GLEnum.Texture2D);
            GLManager.GL.Disable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.Blend);
            GLManager.GL.BlendFunc(GLEnum.SrcAlpha, GLEnum.DstAlpha);
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, flashProgress);
            BlockRenderer.RenderBlockOnInventory(Block.TNT, 0, 1.0F, Tessellator.instance);
            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            GLManager.GL.Disable(GLEnum.Blend);
            GLManager.GL.Enable(GLEnum.Lighting);
            GLManager.GL.Enable(GLEnum.Texture2D);
        }
        GLManager.GL.PopMatrix();
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityTntPrimed)target, x, y, z, yaw, tickDelta);
    }
}
