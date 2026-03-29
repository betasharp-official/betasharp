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
        texture = "/mob/spider.png";
        setBoundingBoxSpacing(1.4F, 0.9F);
        movementSpeed = 0.8F;
    }

    public override void PostSpawn()
    {
        if (world.Random.NextInt(100) == 0)
        {
            EntitySkeleton skeleton = new EntitySkeleton(world);
            skeleton.setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, 0.0F);
            world.SpawnEntity(skeleton);
            skeleton.setVehicle(this);
        }
    }

    public override double getPassengerRidingHeight()
    {
        return (double)height * 0.75D - 0.5D;
    }

    protected override bool bypassesSteppingEffects()
    {
        return false;
    }

    protected override Entity? findPlayerToAttack()
    {
        float brightness = getBrightnessAtEyes(1.0F);
        if (brightness < 0.5F)
        {
            double distance = 16.0D;
            return world.Entities.GetClosestPlayerTarget(this.x, this.y, this.z, distance);
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
        float brightness = getBrightnessAtEyes(1.0F);
        if (brightness > 0.5F && random.NextInt(100) == 0)
        {
            playerToAttack = null;
        }
        else
        {
            if (distance > 2.0F && distance < 6.0F && random.NextInt(10) == 0)
            {
                if (onGround)
                {
                    double dx = entity.x - x;
                    double dz = entity.z - z;
                    float horizontalDistance = MathHelper.Sqrt(dx * dx + dz * dz);
                    velocityX = dx / (double)horizontalDistance * 0.5D * (double)0.8F + velocityX * (double)0.2F;
                    velocityZ = dz / (double)horizontalDistance * 0.5D * (double)0.8F + velocityZ * (double)0.2F;
                    velocityY = (double)0.4F;
                }
            }
            else
            {
                base.attackEntity(entity, distance);
            }

        }
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
    }

    protected override int getDropItemId()
    {
        return Item.String.id;
    }

    public override bool isOnLadder()
    {
        return horizontalCollison;
    }
}
