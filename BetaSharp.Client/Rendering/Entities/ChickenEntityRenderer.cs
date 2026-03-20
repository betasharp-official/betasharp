using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class ChickenEntityRenderer : LivingEntityRenderer
{

    public ChickenEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderChicken(EntityChicken entity, double x, double y, double z, float yaw, float tickDelta)
    {
        base.doRenderLiving(entity, x, y, z, yaw, tickDelta);
    }

    protected float getWingRotation(EntityChicken entity, float partialTicks)
    {
        float wingPhase = entity.field_756_e + (entity.field_752_b - entity.field_756_e) * partialTicks;
        float wingAmplitude = entity.field_757_d + (entity.destPos - entity.field_757_d) * partialTicks;
        return (MathHelper.Sin(wingPhase) + 1.0F) * wingAmplitude;
    }

    protected override float func_170_d(EntityLiving entity, float partialTicks)
    {
        return getWingRotation((EntityChicken)entity, partialTicks);
    }

    public override void doRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        renderChicken((EntityChicken)entity, x, y, z, yaw, tickDelta);
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderChicken((EntityChicken)target, x, y, z, yaw, tickDelta);
    }
}
