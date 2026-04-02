using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;

namespace BetaSharp.Client.Rendering.Entities;

public class CowEntityRenderer : LivingEntityRenderer
{

    public CowEntityRenderer(ModelBase mainModel, float shadowRadius) : base(mainModel, shadowRadius)
    {
    }

    public void renderCow(EntityCow var1, double var2, double var4, double var6, float var8, float var9)
    {
        base.DoRenderLiving(var1, var2, var4, var6, var8, var9);
    }

    public override void DoRenderLiving(EntityLiving var1, double var2, double var4, double var6, float var8, float var9)
    {
        renderCow((EntityCow)var1, var2, var4, var6, var8, var9);
    }

    public override void Render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        renderCow((EntityCow)target, x, y, z, yaw, tickDelta);
    }
}
