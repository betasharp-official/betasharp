using BetaSharp.Blocks;
using BetaSharp.Client.Rendering.Blocks;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Rendering.Entities;

public class FallingBlockEntityRenderer : EntityRenderer
{
    public FallingBlockEntityRenderer()
    {
        ShadowRadius = 0.5F;
    }

    public void doRenderFallingSand(EntityFallingSand var1, double var2, double var4, double var6, float var8, float var9)
    {
        Scene.PushMatrix();
        Scene.Translate((float)var2, (float)var4, (float)var6);
        loadTexture("/terrain.png");
        Block var10 = Block.Blocks[var1.blockId];
        IWorldContext var11 = var1.world;
        Scene.Disable(SceneRenderCapability.Lighting);
        BlockRenderer.RenderBlockFallingSand(var10, var11, MathHelper.Floor(var1.x), MathHelper.Floor(var1.y), MathHelper.Floor(var1.z), Tessellator.instance);
        Scene.Enable(SceneRenderCapability.Lighting);
        Scene.PopMatrix();
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        doRenderFallingSand((EntityFallingSand)target, x, y, z, yaw, tickDelta);
    }
}
