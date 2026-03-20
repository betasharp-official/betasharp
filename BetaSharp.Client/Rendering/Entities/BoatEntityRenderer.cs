using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Entities.Models;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.Rendering.Entities;

public class BoatEntityRenderer : EntityRenderer
{

    protected ModelBase modelBoat;

    public BoatEntityRenderer()
    {
        ShadowRadius = 0.5F;
        modelBoat = new ModelBoat();
    }

    public void render(EntityBoat entity, double x, double y, double z, float yaw, float tickDelta)
    {
        GLManager.GL.PushMatrix();
        GLManager.GL.Translate((float)x, (float)y, (float)z);
        GLManager.GL.Rotate(180.0F - yaw, 0.0F, 1.0F, 0.0F);
        float timeSinceHit = entity.boatTimeSinceHit - tickDelta;
        float currentDamage = entity.boatCurrentDamage - tickDelta;
        if (currentDamage < 0.0F)
        {
            currentDamage = 0.0F;
        }

        if (timeSinceHit > 0.0F)
        {
            GLManager.GL.Rotate(MathHelper.Sin(timeSinceHit) * timeSinceHit * currentDamage / 10.0F * entity.boatRockDirection, 1.0F, 0.0F, 0.0F);
        }

        loadTexture("/terrain.png");
        float scale = 12.0F / 16.0F;
        GLManager.GL.Scale(scale, scale, scale);
        GLManager.GL.Scale(1.0F / scale, 1.0F / scale, 1.0F / scale);
        loadTexture("/item/boat.png");
        GLManager.GL.Scale(-1.0F, -1.0F, 1.0F);
        modelBoat.render(0.0F, 0.0F, -0.1F, 0.0F, 0.0F, 1.0F / 16.0F);
        GLManager.GL.PopMatrix();
    }

    public override void render(Entity target, double x, double y, double z, float yaw, float tickDelta)
    {
        render((EntityBoat)target, x, y, z, yaw, tickDelta);
    }
}