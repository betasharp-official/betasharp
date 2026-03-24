using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityFlameFX : EntityFX
{

    private readonly float baseScale;

    public EntityFlameFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : base(world, x, y, z, velocityX, velocityY, velocityZ)
    {
        base.velocityX = base.velocityX * (double)0.01F + velocityX;
        base.velocityY = base.velocityY * (double)0.01F + velocityY;
        base.velocityZ = base.velocityZ * (double)0.01F + velocityZ;
        baseScale = particleScale;
        particleRed = particleGreen = particleBlue = 1.0F;
        particleMaxAge = (int)(8.0D / (Random.Shared.NextDouble() * 0.8D + 0.2D)) + 4;
        noClip = true;
        particleTextureIndex = 48;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float lifeProgress = ((float)particleAge + partialTick) / (float)particleMaxAge;
        particleScale = baseScale * (1.0F - lifeProgress * lifeProgress * 0.5F);
        base.renderParticle(t, partialTick, rotX, rotY, rotZ, upX, upZ);
    }

    public override float getBrightnessAtEyes(float partialTick)
    {
        float lifeProgress = ((float)particleAge + partialTick) / (float)particleMaxAge;
        if (lifeProgress < 0.0F)
        {
            lifeProgress = 0.0F;
        }

        if (lifeProgress > 1.0F)
        {
            lifeProgress = 1.0F;
        }

        float baseBrightness = base.getBrightnessAtEyes(partialTick);
        return baseBrightness * lifeProgress + (1.0F - lifeProgress);
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
