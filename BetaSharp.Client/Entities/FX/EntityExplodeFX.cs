using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityExplodeFX : EntityFX
{

    public EntityExplodeFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : base(world, x, y, z, velocityX, velocityY, velocityZ)
    {
        base.velocityX = velocityX + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.05F);
        base.velocityY = velocityY + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.05F);
        base.velocityZ = velocityZ + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.05F);
        particleRed = particleGreen = particleBlue = random.NextFloat() * 0.3F + 0.7F;
        particleScale = random.NextFloat() * random.NextFloat() * 6.0F + 1.0F;
        particleMaxAge = (int)(16.0D / ((double)random.NextFloat() * 0.8D + 0.2D)) + 2;
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
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
        velocityY += 0.004D;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.9F;
        velocityY *= (double)0.9F;
        velocityZ *= (double)0.9F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }

    }
}
