using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityHeartFX : EntityFX
{
    readonly float baseScale;


    public EntityHeartFX(IWorldContext world, double x, double y, double z, double motionX, double motionY, double motionZ) : this(world, x, y, z, motionX, motionY, motionZ, 2.0F)
    {
    }

    public EntityHeartFX(IWorldContext world, double x, double y, double z, double motionX, double motionY, double motionZ, float particleScale) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        velocityX *= (double)0.01F;
        velocityY *= (double)0.01F;
        velocityZ *= (double)0.01F;
        velocityY += 0.1D;
        base.particleScale *= 12.0F / 16.0F;
        base.particleScale *= particleScale;
        baseScale = base.particleScale;
        particleMaxAge = 16;
        noClip = false;
        particleTextureIndex = 80;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float lifeProgress = ((float)particleAge + partialTick) / (float)particleMaxAge * 32.0F;
        if (lifeProgress < 0.0F)
        {
            lifeProgress = 0.0F;
        }

        if (lifeProgress > 1.0F)
        {
            lifeProgress = 1.0F;
        }

        particleScale = baseScale * lifeProgress;
        base.renderParticle(t, partialTick, rotX, rotY, rotZ, upX, upZ);
    }

    public override void tick()
    {
        prevX = x;
        prevY = y;
        prevZ = z;
        if (particleAge++ >= particleMaxAge)
        {
            markDead();
        }

        move(velocityX, velocityY, velocityZ);
        if (y == prevY)
        {
            velocityX *= 1.1D;
            velocityZ *= 1.1D;
        }

        velocityX *= (double)0.86F;
        velocityY *= (double)0.86F;
        velocityZ *= (double)0.86F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

    }
}
