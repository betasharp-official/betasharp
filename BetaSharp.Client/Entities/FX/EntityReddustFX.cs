using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityReddustFX : EntityFX
{
    readonly float baseScale;


    public EntityReddustFX(IWorldContext world, double x, double y, double z, float red, float green, float blue) : this(world, x, y, z, 1.0F, red, green, blue)
    {
    }

    public EntityReddustFX(IWorldContext world, double x, double y, double z, float particleScale, float red, float green, float blue) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        velocityX *= (double)0.1F;
        velocityY *= (double)0.1F;
        velocityZ *= (double)0.1F;
        if (red == 0.0F)
        {
            red = 1.0F;
        }

        float colorVariation = (float)Random.Shared.NextDouble() * 0.4F + 0.6F;
        particleRed = ((float)(Random.Shared.NextDouble() * (double)0.2F) + 0.8F) * red * colorVariation;
        particleGreen = ((float)(Random.Shared.NextDouble() * (double)0.2F) + 0.8F) * green * colorVariation;
        particleBlue = ((float)(Random.Shared.NextDouble() * (double)0.2F) + 0.8F) * blue * colorVariation;
        base.particleScale *= 12.0F / 16.0F;
        base.particleScale *= particleScale;
        baseScale = base.particleScale;
        particleMaxAge = (int)(8.0D / (Random.Shared.NextDouble() * 0.8D + 0.2D));
        particleMaxAge = (int)((float)particleMaxAge * particleScale);
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
        move(velocityX, velocityY, velocityZ);
        if (y == prevY)
        {
            velocityX *= 1.1D;
            velocityZ *= 1.1D;
        }

        velocityX *= (double)0.96F;
        velocityY *= (double)0.96F;
        velocityZ *= (double)0.96F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

    }
}
