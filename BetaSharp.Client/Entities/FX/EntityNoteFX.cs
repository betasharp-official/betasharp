using BetaSharp.Client.Rendering.Core;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityNoteFX : EntityFX
{
    readonly float baseScale;


    public EntityNoteFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : this(world, x, y, z, velocityX, velocityY, velocityZ, 2.0F)
    {
    }

    public EntityNoteFX(IWorldContext world, double x, double y, double z, double notePitch, double _, double __, float scaleMultiplier) : base(world, x, y, z, 0.0D, 0.0D, 0.0D)
    {
        velocityX *= (double)0.01F;
        velocityY *= (double)0.01F;
        velocityZ *= (double)0.01F;
        velocityY += 0.2D;
        particleRed = MathHelper.Sin(((float)notePitch + 0.0F) * (float)Math.PI * 2.0F) * 0.65F + 0.35F;
        particleGreen = MathHelper.Sin(((float)notePitch + 1.0F / 3.0F) * (float)Math.PI * 2.0F) * 0.65F + 0.35F;
        particleBlue = MathHelper.Sin(((float)notePitch + 2.0F / 3.0F) * (float)Math.PI * 2.0F) * 0.65F + 0.35F;
        particleScale *= 12.0F / 16.0F;
        particleScale *= scaleMultiplier;
        baseScale = particleScale;
        particleMaxAge = 6;
        noClip = false;
        particleTextureIndex = 64;
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

        velocityX *= (double)0.66F;
        velocityY *= (double)0.66F;
        velocityZ *= (double)0.66F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

    }
}
