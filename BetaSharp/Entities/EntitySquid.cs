using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySquid : EntityWaterMob
{
    public override EntityType Type => EntityRegistry.Squid;
    public float tiltAngle;
    public float prevTiltAngle;
    public float tentaclePhase;
    public float prevTentaclePhase;
    public float swimPhase;
    public float prevSwimPhase;
    public float tentacleSpread;
    public float prevTentacleSpread;
    private float randomMotionSpeed;
    private float animationSpeed;
    private float squidRotation;
    private float randomMotionVecX;
    private float randomMotionVecY;
    private float randomMotionVecZ;

    public EntitySquid(IWorldContext world) : base(world)
    {
        Texture = "/mob/squid.png";
        SetBoundingBoxSpacing(0.95F, 0.95F);
        animationSpeed = 1.0F / (Random.NextFloat() + 1.0F) * 0.2F;
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
    }

    protected override String getLivingSound()
    {
        return null;
    }

    protected override String getHurtSound()
    {
        return null;
    }

    protected override String getDeathSound()
    {
        return null;
    }

    protected override float getSoundVolume()
    {
        return 0.4F;
    }

    protected override int getDropItemId()
    {
        return 0;
    }

    protected override void dropFewItems()
    {
        int dropCount = Random.NextInt(3) + 1;

        for (int _ = 0; _ < dropCount; ++_)
        {
            DropItem(new ItemStack(Item.Dye, 1, 0), 0.0F);
        }

    }

    public override bool Interact(EntityPlayer player)
    {
        return false;
    }

    public override bool IsInWater()
    {
        return World.Reader.UpdateMovementInFluid(BoundingBox.Expand(0.0D, (double)-0.6F, 0.0D), Material.Water, this);
    }

    public override void tickMovement()
    {
        base.tickMovement();
        prevTiltAngle = tiltAngle;
        prevTentaclePhase = tentaclePhase;
        prevSwimPhase = swimPhase;
        prevTentacleSpread = tentacleSpread;
        swimPhase += animationSpeed;
        if (swimPhase > (float)Math.PI * 2.0F)
        {
            swimPhase -= (float)Math.PI * 2.0F;
            if (Random.NextInt(10) == 0)
            {
                animationSpeed = 1.0F / (Random.NextFloat() + 1.0F) * 0.2F;
            }
        }

        if (IsInWater())
        {
            float phaseProgress;
            if (swimPhase < (float)Math.PI)
            {
                phaseProgress = swimPhase / (float)Math.PI;
                tentacleSpread = MathHelper.Sin(phaseProgress * phaseProgress * (float)Math.PI) * (float)Math.PI * 0.25F;
                if ((double)phaseProgress > 0.75D)
                {
                    randomMotionSpeed = 1.0F;
                    squidRotation = 1.0F;
                }
                else
                {
                    squidRotation *= 0.8F;
                }
            }
            else
            {
                tentacleSpread = 0.0F;
                randomMotionSpeed *= 0.9F;
                squidRotation *= 0.99F;
            }

            if (!InterpolateOnly)
            {
                VelocityX = (double)(randomMotionVecX * randomMotionSpeed);
                VelocityY = (double)(randomMotionVecY * randomMotionSpeed);
                VelocityZ = (double)(randomMotionVecZ * randomMotionSpeed);
            }

            phaseProgress = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            BodyYaw += (-((float)System.Math.Atan2(VelocityX, VelocityZ)) * 180.0F / (float)Math.PI - BodyYaw) * 0.1F;
            Yaw = BodyYaw;
            tentaclePhase += (float)Math.PI * squidRotation * 1.5F;
            tiltAngle += (-((float)System.Math.Atan2((double)phaseProgress, VelocityY)) * 180.0F / (float)Math.PI - tiltAngle) * 0.1F;
        }
        else
        {
            tentacleSpread = MathHelper.Abs(MathHelper.Sin(swimPhase)) * (float)Math.PI * 0.25F;
            if (!InterpolateOnly)
            {
                VelocityX = 0.0D;
                VelocityY -= 0.08D;
                VelocityY *= (double)0.98F;
                VelocityZ = 0.0D;
            }

            tiltAngle = (float)((double)tiltAngle + (double)(-90.0F - tiltAngle) * 0.02D);
        }

    }

    public override void travel(float strafe, float forward)
    {
        Move(VelocityX, VelocityY, VelocityZ);
    }

    public override void tickLiving()
    {
        if (Random.NextInt(50) == 0 || !InWater || randomMotionVecX == 0.0F && randomMotionVecY == 0.0F && randomMotionVecZ == 0.0F)
        {
            float randomAngle = Random.NextFloat() * (float)Math.PI * 2.0F;
            randomMotionVecX = MathHelper.Cos(randomAngle) * 0.2F;
            randomMotionVecY = -0.1F + Random.NextFloat() * 0.2F;
            randomMotionVecZ = MathHelper.Sin(randomAngle) * 0.2F;
        }

        func_27021_X();
    }
}
