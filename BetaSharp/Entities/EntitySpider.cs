using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySpider : EntityMonster
{
    public override EntityType Type => EntityRegistry.Spider;
    public EntitySpider(IWorldContext world) : base(world)
    {
        Texture = "/mob/spider.png";
        SetBoundingBoxSpacing(1.4F, 0.9F);
        MovementSpeed = 0.8F;
    }

    public override void PostSpawn()
    {
        if (World.Random.NextInt(100) == 0)
        {
            EntitySkeleton skeleton = new EntitySkeleton(World);
            skeleton.SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, 0.0F);
            World.SpawnEntity(skeleton);
            skeleton.SetVehicle(this);
        }
    }

    public override double GetPassengerRidingHeight()
    {
        return (double)Height * 0.75D - 0.5D;
    }

    protected override bool BypassesSteppingEffects()
    {
        return false;
    }

    protected override Entity? findPlayerToAttack()
    {
        float brightness = GetBrightnessAtEyes(1.0F);
        if (brightness < 0.5F)
        {
            double distance = 16.0D;
            return World.Entities.GetClosestPlayerTarget(this.X, this.Y, this.Z, distance);
        }
        else
        {
            return null;
        }
    }

    protected override string getLivingSound()
    {
        return "mob.spider";
    }

    protected override string getHurtSound()
    {
        return "mob.spider";
    }

    protected override string getDeathSound()
    {
        return "mob.spiderdeath";
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        float brightness = GetBrightnessAtEyes(1.0F);
        if (brightness > 0.5F && Random.NextInt(100) == 0)
        {
            playerToAttack = null;
        }
        else
        {
            if (distance > 2.0F && distance < 6.0F && Random.NextInt(10) == 0)
            {
                if (OnGround)
                {
                    double dx = entity.X - X;
                    double dz = entity.Z - Z;
                    float horizontalDistance = MathHelper.Sqrt(dx * dx + dz * dz);
                    VelocityX = dx / (double)horizontalDistance * 0.5D * (double)0.8F + VelocityX * (double)0.2F;
                    VelocityZ = dz / (double)horizontalDistance * 0.5D * (double)0.8F + VelocityZ * (double)0.2F;
                    VelocityY = (double)0.4F;
                }
            }
            else
            {
                base.attackEntity(entity, distance);
            }

        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
    }

    protected override int getDropItemId()
    {
        return Item.String.id;
    }

    public override bool isOnLadder()
    {
        return HorizontalCollison;
    }
}
