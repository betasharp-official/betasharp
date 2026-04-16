using BetaSharp.Client.Rendering.Core;
using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Client.Entities.FX;

public class EntityFX : Entity
{
    public override EntityType? Type => null;

    protected int particleTextureIndex;
    protected float particleTextureJitterX;
    protected float particleTextureJitterY;
    protected int particleAge;
    protected int particleMaxAge;
    protected float particleScale;
    protected float particleGravity;
    protected float particleRed;
    protected float particleGreen;
    protected float particleBlue;
    public static double interpPosX;
    public static double interpPosY;
    public static double interpPosZ;

    public EntityFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY,
        double velocityZ) : base(world)
    {
        setBoundingBoxSpacing(0.2F, 0.2F);
        standingEyeHeight = height / 2.0F;
        setPosition(x, y, z);
        particleRed = particleGreen = particleBlue = 1.0F;
        base.velocityX = velocityX + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.4F);
        base.velocityY = velocityY + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.4F);
        base.velocityZ = velocityZ + (double)((float)(Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.4F);
        float velocityScale = (float)(Random.Shared.NextDouble() + Random.Shared.NextDouble() + 1.0D) * 0.15F;
        float speed = MathHelper.Sqrt(base.velocityX * base.velocityX + base.velocityY * base.velocityY +
                                      base.velocityZ * base.velocityZ);
        base.velocityX = base.velocityX / (double)speed * (double)velocityScale * (double)0.4F;
        base.velocityY = base.velocityY / (double)speed * (double)velocityScale * (double)0.4F + (double)0.1F;
        base.velocityZ = base.velocityZ / (double)speed * (double)velocityScale * (double)0.4F;
        particleTextureJitterX = random.NextFloat() * 3.0F;
        particleTextureJitterY = random.NextFloat() * 3.0F;
        particleScale = (random.NextFloat() * 0.5F + 0.5F) * 2.0F;
        particleMaxAge = (int)(4.0F / (random.NextFloat() * 0.9F + 0.1F));
        particleAge = 0;
    }

    public EntityFX scaleVelocity(float multiplier)
    {
        velocityX *= (double)multiplier;
        velocityY = (velocityY - (double)0.1F) * (double)multiplier + (double)0.1F;
        velocityZ *= (double)multiplier;
        return this;
    }

    public EntityFX scaleSize(float scale)
    {
        setBoundingBoxSpacing(0.2F * scale, 0.2F * scale);
        particleScale *= scale;
        return this;
    }

    protected override bool bypassesSteppingEffects()
    {
        return false;
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

        velocityY -= 0.04D * (double)particleGravity;
        move(velocityX, velocityY, velocityZ);
        velocityX *= (double)0.98F;
        velocityY *= (double)0.98F;
        velocityZ *= (double)0.98F;
        if (onGround)
        {
            velocityX *= (double)0.7F;
            velocityZ *= (double)0.7F;
        }
    }

    public virtual void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX,
        float upZ)
    {
        float minU = (float)(particleTextureIndex % 16) / 16.0F;
        float maxU = minU + 0.999F / 16.0F;
        float minV = (float)(particleTextureIndex / 16) / 16.0F;
        float maxV = minV + 0.999F / 16.0F;
        float size = 0.1F * particleScale;
        float x = (float)(prevX + (base.x - prevX) * (double)partialTick - interpPosX);
        float y = (float)(prevY + (base.y - prevY) * (double)partialTick - interpPosY);
        float z = (float)(prevZ + (base.z - prevZ) * (double)partialTick - interpPosZ);
        float brightness = getBrightnessAtEyes(partialTick);
        t.setColorOpaque_F(particleRed * brightness, particleGreen * brightness, particleBlue * brightness);
        t.addVertexWithUV((double)(x - rotX * size - upX * size), (double)(y - rotY * size),
            (double)(z - rotZ * size - upZ * size), (double)maxU, (double)maxV);
        t.addVertexWithUV((double)(x - rotX * size + upX * size), (double)(y + rotY * size),
            (double)(z - rotZ * size + upZ * size), (double)maxU, (double)minV);
        t.addVertexWithUV((double)(x + rotX * size + upX * size), (double)(y + rotY * size),
            (double)(z + rotZ * size + upZ * size), (double)minU, (double)minV);
        t.addVertexWithUV((double)(x + rotX * size - upX * size), (double)(y - rotY * size),
            (double)(z + rotZ * size - upZ * size), (double)minU, (double)maxV);
    }

    public virtual int getFXLayer()
    {
        return 0;
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
    }

    public override void readNbt(NBTTagCompound nbt)
    {
    }
}
