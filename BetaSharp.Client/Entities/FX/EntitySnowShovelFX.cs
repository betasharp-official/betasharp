using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntitySnowShovelFX : EntityFX
{
    readonly float baseScale;


    public EntitySnowShovelFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : this(world, x, y, z, velocityX, velocityY, velocityZ, 1.0F)
    {
    }

    public EntitySnowShovelFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ, float scaleMultiplier) : base(world, x, y, z, velocityX, velocityY, velocityZ)
    {
        base.velocityX *= (double)0.1F;
        base.velocityY *= (double)0.1F;
        base.velocityZ *= (double)0.1F;
        base.velocityX += velocityX;
        base.velocityY += velocityY;
        base.velocityZ += velocityZ;
        particleRed = particleGreen = particleBlue = 1.0F - (float)(Random.Shared.NextDouble() * (double)0.3F);
        particleScale *= 12.0F / 16.0F;
        particleScale *= scaleMultiplier;
        baseScale = particleScale;
        particleMaxAge = (int)(8.0D / (Random.Shared.NextDouble() * 0.8D + 0.2D));
        particleMaxAge = (int)((float)particleMaxAge * scaleMultiplier);
        noClip = false;
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

        particleTextureIndex = 7 - particleAge * 8 / particleMaxAge;
        velocityY -= 0.03D;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.99F;
        velocityY *= (double)0.99F;
        velocityZ *= (double)0.99F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

    }
}
