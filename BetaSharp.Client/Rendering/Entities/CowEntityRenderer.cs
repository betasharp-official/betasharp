using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class CowEntityRenderer : LivingEntityRenderer
{

    public CowEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderCow(EntityCow cowEntity, double x, double y, double z, float yaw, float tickDelta)
    {
        base.DoRenderLiving(cowEntity, x, y, z, yaw, tickDelta);
    }

    public override void DoRenderLiving(EntityLiving entity, double x, double y, double z, float yaw, float tickDelta)
    {
        renderCow((EntityCow)entity, x, y, z, yaw, tickDelta);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderCow((EntityCow)target, x, y, z, yaw, tickDelta);
    }
}
