using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySkeleton : EntityMonster
{
    public override EntityType Type => EntityRegistry.Skeleton;
    private static readonly ItemStack defaultHeldItem = new ItemStack(Item.BOW, 1);

    public EntitySkeleton(IWorldContext world) : base(world)
    {
        texture = "/mob/skeleton.png";
    }

    protected override String getLivingSound()
    {
        return "mob.skeleton";
    }

    protected override String getHurtSound()
    {
        return "mob.skeletonhurt";
    }

    protected override String getDeathSound()
    {
        return "mob.skeletonhurt";
    }

    public override void tickMovement()
    {
        if (world.Environment.CanMonsterSpawn())
        {
            float brightness = getBrightnessAtEyes(1.0F);
            if (brightness > 0.5F && world.Lighting.HasSkyLight(MathHelper.Floor(x), MathHelper.Floor(y), MathHelper.Floor(z)) && random.NextFloat() * 30.0F < (brightness - 0.4F) * 2.0F)
            {
                fireTicks = 300;
            }
        }

        base.tickMovement();
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (distance < 10.0F)
        {
            double dx = entity.x - x;
            double dy = entity.z - z;
            if (attackTime == 0)
            {
                EntityArrow arrow = new EntityArrow(world, this);
                arrow.y += (double)1.4F;
                double targetHeightOffset = entity.y + (double)entity.getEyeHeight() - (double)0.2F - arrow.y;
                float distanceFactor = MathHelper.Sqrt(dx * dx + dy * dy) * 0.2F;
                world.Broadcaster.PlaySoundAtEntity(this, "random.bow", 1.0F, 1.0F / (random.NextFloat() * 0.4F + 0.8F));
                world.SpawnEntity(arrow);
                arrow.setArrowHeading(dx, targetHeightOffset + (double)distanceFactor, dy, 0.6F, 12.0F);
                attackTime = 30;
            }

            yaw = (float)(System.Math.Atan2(dy, dx) * 180.0D / (double)((float)Math.PI)) - 90.0F;
            hasAttacked = true;
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
        return Item.ARROW.id;
    }

    protected override void dropFewItems()
    {
        int amount = random.NextInt(3);

        int i;
        for (i = 0; i < amount; ++i)
        {
            dropItem(Item.ARROW.id, 1);
        }

        amount = random.NextInt(3);

        for (i = 0; i < amount; ++i)
        {
            dropItem(Item.Bone.id, 1);
        }

    }

    public override ItemStack getHeldItem()
    {
        return defaultHeldItem;
    }
}
