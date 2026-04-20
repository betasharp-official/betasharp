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

    public EntityFX(IWorldContext world, double x, double y, double z, double velocityX, double velocityY, double velocityZ) : base(world)
    {
        SetBoundingBoxSpacing(0.2F, 0.2F);
        StandingEyeHeight = Height / 2.0F;
        SetPosition(x, y, z);
        particleRed = particleGreen = particleBlue = 1.0F;
        base.VelocityX = velocityX + (double)((float)(System.Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.4F);
        base.VelocityY = velocityY + (double)((float)(System.Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.4F);
        base.VelocityZ = velocityZ + (double)((float)(System.Random.Shared.NextDouble() * 2.0D - 1.0D) * 0.4F);
        float velocityScale = (float)(System.Random.Shared.NextDouble() + System.Random.Shared.NextDouble() + 1.0D) * 0.15F;
        float speed = MathHelper.Sqrt(base.VelocityX * base.VelocityX + base.VelocityY * base.VelocityY + base.VelocityZ * base.VelocityZ);
        base.VelocityX = base.VelocityX / (double)speed * (double)velocityScale * (double)0.4F;
        base.VelocityY = base.VelocityY / (double)speed * (double)velocityScale * (double)0.4F + (double)0.1F;
        base.VelocityZ = base.VelocityZ / (double)speed * (double)velocityScale * (double)0.4F;
        particleTextureJitterX = Random.NextFloat() * 3.0F;
        particleTextureJitterY = Random.NextFloat() * 3.0F;
        particleScale = (Random.NextFloat() * 0.5F + 0.5F) * 2.0F;
        particleMaxAge = (int)(4.0F / (Random.NextFloat() * 0.9F + 0.1F));
        particleAge = 0;
    }

    public EntityFX scaleVelocity(float multiplier)
    {
        VelocityX *= (double)multiplier;
        VelocityY = (VelocityY - (double)0.1F) * (double)multiplier + (double)0.1F;
        VelocityZ *= (double)multiplier;
        return this;
    }

    public EntityFX scaleSize(float scale)
    {
        SetBoundingBoxSpacing(0.2F * scale, 0.2F * scale);
        particleScale *= scale;
        return this;
    }

    protected override bool BypassesSteppingEffects()
    {
        return false;
    }

    public override void Tick()
    {
        PrevX = X;
        PrevY = Y;
        PrevZ = Z;
        if (particleAge++ >= particleMaxAge)
        {
            MarkDead();
        }

        VelocityY -= 0.04D * (double)particleGravity;
        Move(VelocityX, VelocityY, VelocityZ);
        VelocityX *= (double)0.98F;
        VelocityY *= (double)0.98F;
        VelocityZ *= (double)0.98F;
        if (OnGround)
        {
            VelocityX *= (double)0.7F;
            VelocityZ *= (double)0.7F;
        }

    }

    public virtual void renderParticle(Tessellator t, float partialTick, float rotX, float rotY, float rotZ, float upX, float upZ)
    {
        float minU = (float)(particleTextureIndex % 16) / 16.0F;
        float maxU = minU + 0.999F / 16.0F;
        float minV = (float)(particleTextureIndex / 16) / 16.0F;
        float maxV = minV + 0.999F / 16.0F;
        float size = 0.1F * particleScale;
        float x = (float)(PrevX + (base.X - PrevX) * (double)partialTick - interpPosX);
        float y = (float)(PrevY + (base.Y - PrevY) * (double)partialTick - interpPosY);
        float z = (float)(PrevZ + (base.Z - PrevZ) * (double)partialTick - interpPosZ);
        float brightness = GetBrightnessAtEyes(partialTick);
        t.setColorOpaque_F(particleRed * brightness, particleGreen * brightness, particleBlue * brightness);
        t.addVertexWithUV((double)(x - rotX * size - upX * size), (double)(y - rotY * size), (double)(z - rotZ * size - upZ * size), (double)maxU, (double)maxV);
        t.addVertexWithUV((double)(x - rotX * size + upX * size), (double)(y + rotY * size), (double)(z - rotZ * size + upZ * size), (double)maxU, (double)minV);
        t.addVertexWithUV((double)(x + rotX * size + upX * size), (double)(y + rotY * size), (double)(z + rotZ * size + upZ * size), (double)minU, (double)minV);
        t.addVertexWithUV((double)(x + rotX * size - upX * size), (double)(y - rotY * size), (double)(z + rotZ * size - upZ * size), (double)minU, (double)maxV);
    }

    public virtual int getFXLayer()
    {
        return 0;
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
    }
}
