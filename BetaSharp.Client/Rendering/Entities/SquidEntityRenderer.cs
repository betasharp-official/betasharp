using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class SquidEntityRenderer : LivingEntityRenderer
{

    public SquidEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void func_21008_a(EntitySquid squidEntity, double x, double y, double z, float yaw, float tickDelta)
    {
        base.DoRenderLiving(squidEntity, x, y, z, yaw, tickDelta);
    }

    protected void func_21007_a(EntitySquid squidEntity, float deathTime, float bodyYaw, float tickDelta)
    {
        float tiltAngle = squidEntity.PrevTiltAngle + (squidEntity.TiltAngle - squidEntity.PrevTiltAngle) * tickDelta;
        float tentacleYaw = squidEntity.PrevTentaclePhase + (squidEntity.TentaclePhase - squidEntity.PrevTentaclePhase) * tickDelta;
        GLManager.GL.Translate(0.0F, 0.5F, 0.0F);
        GLManager.GL.Rotate(180.0F - bodyYaw, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Rotate(tiltAngle, 1.0F, 0.0F, 0.0F);
        GLManager.GL.Rotate(tentacleYaw, 0.0F, 1.0F, 0.0F);
        GLManager.GL.Translate(0.0F, -1.2F, 0.0F);
    }

    protected void func_21005_a(EntitySquid squidEntity, float tickDelta)
    {
    }

    protected float func_21006_b(EntitySquid squidEntity, float tickDelta)
    {
        float tentacleSpread = squidEntity.PrevTentacleSpread + (squidEntity.TentacleSpread - squidEntity.PrevTentacleSpread) * tickDelta;
        return tentacleSpread;
    }

    protected override void PreRenderCallback(EntityLiving entity, float tickDelta)
    {
        func_21005_a((EntitySquid)entity, tickDelta);
    }

    protected override float getAnimationProgress(EntityLiving entity, float tickDelta)
    {
        return func_21006_b((EntitySquid)entity, tickDelta);
    }

    protected override void RotateCorpse(EntityLiving entity, float deathTime, float bodyYaw, float tickDelta)
    {
        func_21007_a((EntitySquid)entity, deathTime, bodyYaw, tickDelta);
    }

    public override void DoRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        func_21008_a((EntitySquid)entity, x, y, z, yaw, tickDelta);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        func_21008_a((EntitySquid)target, x, y, z, yaw, tickDelta);
    }
}
