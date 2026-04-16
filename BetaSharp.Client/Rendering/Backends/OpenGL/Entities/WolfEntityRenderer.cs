using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class WolfEntityRenderer : LivingEntityRenderer
{
    public WolfEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderWolf(EntityWolf var1, double var2, double var4, double var6, float var8, float var9)
    {
        base.DoRenderLiving(var1, var2, var4, var6, var8, var9);
    }

    protected float func_25004_a(EntityWolf var1, float var2)
    {
        return var1.getTailRotation();
    }

    protected void func_25006_b(EntityWolf var1, float var2)
    {
    }

    protected override void PreRenderCallback(EntityLiving var1, float var2)
    {
        func_25006_b((EntityWolf)var1, var2);
    }

    protected override float getAnimationProgress(EntityLiving var1, float var2)
    {
        return func_25004_a((EntityWolf)var1, var2);
    }

    public override void DoRenderLiving(EntityLiving var1, double var2, double var4, double var6, float var8,
        float var9)
    {
        renderWolf((EntityWolf)var1, var2, var4, var6, var8, var9);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderWolf((EntityWolf)target, x, y, z, yaw, tickDelta);
    }
}
