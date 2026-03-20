using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class CowEntityRenderer : LivingEntityRenderer
{

    public CowEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderCow(EntityCow entity, double x, double y, double z, float yaw, float tickDelta)
    {
        base.doRenderLiving(entity, x, y, z, yaw, tickDelta);
    }

    public override void doRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        renderCow((EntityCow)entity, x, y, z, yaw, tickDelta);
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderCow((EntityCow)target, x, y, z, yaw, tickDelta);
    }
}