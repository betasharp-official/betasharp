using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SquidEntityRenderer : LivingEntityRenderer
{

    public SquidEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void func_21008_a(EntitySquid var1, double var2, double var4, double var6, float var8, float var9)
    {
        base.doRenderLiving(var1, var2, var4, var6, var8, var9);
    }

    protected void func_21007_a(EntitySquid var1, float var2, float var3, float var4)
    {
        float var5 = var1.prevTiltAngle + (var1.tiltAngle - var1.prevTiltAngle) * var4;
        float var6 = var1.prevTentaclePhase + (var1.tentaclePhase - var1.prevTentaclePhase) * var4;
        GLManager.GL.Translate(0.0F, 0.5F, 0.0F);
        GLManager.GL.Rotate(180.0F - var3, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(var5, 1.0F, 0.0F, 0.0F);
        GLManager.GL.Rotate(var6, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Translate(0.0F, -1.2F, 0.0F);
    }

    protected void func_21005_a(EntitySquid var1, float var2)
    {
    }

    protected float func_21006_b(EntitySquid var1, float var2)
    {
        float var3 = var1.prevTentacleSpread + (var1.tentacleSpread - var1.prevTentacleSpread) * var2;
        return var3;
    }

    protected override void preRenderCallback(EntityLiving var1, float var2)
    {
        func_21005_a((EntitySquid)var1, var2);
    }

    protected override float getAnimationProgress(EntityLiving var1, float var2)
    {
        return func_21006_b((EntitySquid)var1, var2);
    }

    protected override void rotateCorpse(EntityLiving var1, float var2, float var3, float var4)
    {
        func_21007_a((EntitySquid)var1, var2, var3, var4);
    }

    public override void doRenderLiving(EntityLiving var1, double var2, double var4, double var6, float var8, float var9)
    {
        func_21008_a((EntitySquid)var1, var2, var4, var6, var8, var9);
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        func_21008_a((EntitySquid)target, x, y, z, yaw, tickDelta);
    }
}