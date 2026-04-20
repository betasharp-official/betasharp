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
        Texture = "/mob/skeleton.png";
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
        if (World.Environment.CanMonsterSpawn())
        {
            float brightness = GetBrightnessAtEyes(1.0F);
            if (brightness > 0.5F && World.Lighting.HasSkyLight(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z)) && Random.NextFloat() * 30.0F < (brightness - 0.4F) * 2.0F)
            {
                FireTicks = 300;
            }
        }

        base.tickMovement();
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (distance < 10.0F)
        {
            double dx = entity.X - X;
            double dy = entity.Z - Z;
            if (AttackTime == 0)
            {
                EntityArrow arrow = new EntityArrow(World, this);
                double targetHeightOffset = entity.Y + (double)entity.GetEyeHeight() - (double)0.2F - arrow.Y;
                float distanceFactor = MathHelper.Sqrt(dx * dx + dy * dy) * 0.2F;
                World.Broadcaster.PlaySoundAtEntity(this, "random.bow", 1.0F, 1.0F / (Random.NextFloat() * 0.4F + 0.8F));
                World.SpawnEntity(arrow);
                arrow.setArrowHeading(dx, targetHeightOffset + (double)distanceFactor, dy, 0.6F, 12.0F);
                AttackTime = 30;
            }

            Yaw = (float)(System.Math.Atan2(dy, dx) * 180.0D / (double)((float)Math.PI)) - 90.0F;
            hasAttacked = true;
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
        return Item.ARROW.id;
    }

    protected override void dropFewItems()
    {
        int amount = Random.NextInt(3);

        int i;
        for (i = 0; i < amount; ++i)
        {
            DropItem(Item.ARROW.id, 1);
        }

        amount = Random.NextInt(3);

        for (i = 0; i < amount; ++i)
        {
            DropItem(Item.Bone.id, 1);
        }

    }

    public override ItemStack getHeldItem()
    {
        return defaultHeldItem;
    }
}
