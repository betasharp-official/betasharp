using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Rendering.Entities;

public class FallingBlockEntityRenderer : EntityRenderer
{
    public FallingBlockEntityRenderer()
    {
        ShadowRadius = 0.5F;
    }

    public void doRenderFallingSand(EntityFallingSand var1, double var2, double var4, double var6, float var8, float var9)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)var2, (float)var4, (float)var6);
        loadTexture("/terrain.png");
        Block var10 = Block.Blocks[var1.blockId];
        World var11 = var1.getWorld();
        GLManager.GL.Disable(GLEnum.Lighting);
        BlockRenderer.RenderBlockFallingSand(var10, var11, MathHelper.Floor(var1.x), MathHelper.Floor(var1.y), MathHelper.Floor(var1.z), Tessellator.instance);
        GLManager.GL.Enable(GLEnum.Lighting);
        GLManager.GL.PopMatrix();
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderFallingSand((EntityFallingSand)target, x, y, z, yaw, tickDelta);
    }
}
