using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Client.Entities.FX;

public class EntityLavaFX : EntityFX
{

    private readonly float baseScale;

    public EntityLavaFX(World world, double x, double y, double z) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        velocityX *= (double)0.8F;
        velocityY *= (double)0.8F;
        velocityZ *= (double)0.8F;
        velocityY = (double)(random.NextFloat() * 0.4F + 0.05F);
        particleRed = particleGreen = particleBlue = 1.0F;
        particleScale *= random.NextFloat() * 2.0F + 0.2F;
        baseScale = particleScale;
        particleMaxAge = (int)(16.0D / (Random.Shared.NextDouble() * 0.8D + 0.2D));
        noClip = false;
        particleTextureIndex = 49;
    }

    public override float getBrightnessAtEyes(float brightness)
    {
        return 1.0F;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float lifeProgress = ((float)particleAge + partialTick) / (float)particleMaxAge;
        particleScale = baseScale * (1.0F - lifeProgress * lifeProgress);
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

        float lifeProgress = (float)particleAge / (float)particleMaxAge;
        if (random.NextFloat() > lifeProgress)
        {
            world.Broadcaster.AddParticle("smoke", x, y, z, velocityX, velocityY, velocityZ);
        }

        velocityY -= 0.03D;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.999F;
        velocityY *= (double)0.999F;
        velocityZ *= (double)0.999F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

    }
}
