using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySpider : EntityMonster
{
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
            EntitySkeleton skeleton = new(world);
            skeleton.setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, 0.0F);
            world.SpawnEntity(skeleton);
            skeleton.setVehicle(this);
        }
    }

    public override double getPassengerRidingHeight() => height * 0.75D - 0.5D;

    protected override bool bypassesSteppingEffects() => false;

    protected override Entity findPlayerToAttack()
    {
        float brightness = getBrightnessAtEyes(1.0F);
        if (brightness < 0.5F)
        {
            double distance = 16.0D;
            return world.Entities.GetClosestPlayer(x, y, z, distance);
        }

        return null;
    }

    protected override string getLivingSound() => "mob.spider";

    protected override string getHurtSound() => "mob.spider";

    protected override string getDeathSound() => "mob.spiderdeath";

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
                    velocityX = dx / horizontalDistance * 0.5D * 0.8F + velocityX * 0.2F;
                    velocityZ = dz / horizontalDistance * 0.5D * 0.8F + velocityZ * 0.2F;
                    velocityY = 0.4F;
                }
            }
            else
            {
                base.attackEntity(entity, distance);
            }
        }
    }

    public override void writeNbt(NBTTagCompound nbt) => base.writeNbt(nbt);

    public override void readNbt(NBTTagCompound nbt) => base.readNbt(nbt);

    protected override int getDropItemId() => Item.String.id;

    public override bool isOnLadder() => horizontalCollison;
}
