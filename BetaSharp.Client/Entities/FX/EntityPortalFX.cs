using BetaSharp.Client.Rendering.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityPortalFX : EntityFX
{

    private readonly float baseScale;
    private readonly double spawnX;
    private readonly double spawnY;
    private readonly double spawnZ;

    public EntityPortalFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : base(world, x, y, z, velocityX, velocityY, velocityZ)
    {
        base.velocityX = velocityX;
        base.velocityY = velocityY;
        base.velocityZ = velocityZ;
        spawnX = base.x = x;
        spawnY = base.y = y;
        spawnZ = base.z = z;
        float brightnessVariation = random.NextFloat() * 0.6F + 0.4F;
        baseScale = particleScale = random.NextFloat() * 0.2F + 0.5F;
        particleRed = particleGreen = particleBlue = 1.0F * brightnessVariation;
        particleGreen *= 0.3F;
        particleRed *= 0.9F;
        particleMaxAge = (int)(Random.Shared.NextDouble() * 10.0D) + 40;
        noClip = true;
        particleTextureIndex = (int)(Random.Shared.NextDouble() * 8.0D);
    }

    public override void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float lifeProgress = ((float)particleAge + partialTick) / (float)particleMaxAge;
        lifeProgress = 1.0F - lifeProgress;
        lifeProgress *= lifeProgress;
        lifeProgress = 1.0F - lifeProgress;
        particleScale = baseScale * lifeProgress;
        base.renderParticle(t, partialTick, rotX, rotY, rotZ, upX, upZ);
    }

    public override float getBrightnessAtEyes(float partialTick)
    {
        float worldBrightness = base.getBrightnessAtEyes(partialTick);
        float lifeProgress = (float)particleAge / (float)particleMaxAge;
        lifeProgress *= lifeProgress;
        lifeProgress *= lifeProgress;
        return worldBrightness * (1.0F - lifeProgress) + lifeProgress;
    }

    public override void tick()
    {
        prevX = x;
        prevY = y;
        prevZ = z;
        float progressFactor = (float)particleAge / (float)particleMaxAge;
        float lifeProgress = progressFactor;
        progressFactor = -progressFactor + progressFactor * progressFactor * 2.0F;
        progressFactor = 1.0F - progressFactor;
        x = spawnX + velocityX * (double)progressFactor;
        y = spawnY + velocityY * (double)progressFactor + (double)(1.0F - lifeProgress);
        z = spawnZ + velocityZ * (double)progressFactor;
        if (particleAge++ >= particleMaxAge)
        {
            markDead();
        }

    }
}
