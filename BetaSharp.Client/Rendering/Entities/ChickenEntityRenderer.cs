using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class ChickenEntityRenderer : LivingEntityRenderer
{

    public ChickenEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderChicken(EntityChicken chicken, double x, double y, double z, float yaw, float tickDelta)
    {
        base.DoRenderLiving(chicken, x, y, z, yaw, tickDelta);
    }

    protected float getWingRotation(EntityChicken chicken, float tickDelta)
    {
        float flapProgress = chicken.PrevFlapProgress + (chicken.FlapProgress - chicken.PrevFlapProgress) * tickDelta;
        float wingExtension = chicken.PrevDestPos + (chicken.DestPos - chicken.PrevDestPos) * tickDelta;
        return (MathHelper.Sin(flapProgress) + 1.0F) * wingExtension;
    }

    protected override float getAnimationProgress(EntityLiving entity, float tickDelta)
    {
        return getWingRotation((EntityChicken)entity, tickDelta);
    }

    public override void DoRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        renderChicken((EntityChicken)entity, x, y, z, yaw, tickDelta);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderChicken((EntityChicken)target, x, y, z, yaw, tickDelta);
    }
}
