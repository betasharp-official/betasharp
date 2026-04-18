using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class WolfEntityRenderer : LivingEntityRenderer
{

    public WolfEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderWolf(EntityWolf wolfEntity, double x, double y, double z, float yaw, float tickDelta)
    {
        base.DoRenderLiving(wolfEntity, x, y, z, yaw, tickDelta);
    }

    protected float func_25004_a(EntityWolf wolf, float tickDelta)
    {
        return wolf.GetTailRotation();
    }

    protected void func_25006_b(EntityWolf wolf, float tickDelta)
    {
    }

    protected override void PreRenderCallback(EntityLiving entity, float tickDelta)
    {
        func_25006_b((EntityWolf)entity, tickDelta);
    }

    protected override float getAnimationProgress(EntityLiving entity, float tickDelta)
    {
        return func_25004_a((EntityWolf)entity, tickDelta);
    }

    public override void DoRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        renderWolf((EntityWolf)entity, x, y, z, yaw, tickDelta);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderWolf((EntityWolf)target, x, y, z, yaw, tickDelta);
    }
}
