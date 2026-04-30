using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySkeleton : EntityMonster
{
    private static readonly ItemStack s_defaultHeldItem = new(Item.BOW, 1);

    public EntitySkeleton(IWorldContext world) : base(world) => Texture = "/mob/skeleton.png";
    public override EntityType Type => EntityRegistry.Skeleton;

    protected override string? LivingSound => "mob.skeleton";

    protected override string? HurtSound => "mob.skeletonhurt";

    protected override string? DeathSound => "mob.skeletonhurt";

    public override ItemStack HeldItem => s_defaultHeldItem;

    protected override void TickMovement()
    {
        if (World.Environment.CanMonsterSpawn())
        {
            float brightness = GetBrightnessAtEyes(1.0F);
            if (brightness > 0.5F && World.Lighting.HasSkyLight(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z)) && Random.NextFloat() * 30.0F < (brightness - 0.4F) * 2.0F)
            {
                FireTicks = 300;
            }
        }

        base.TickMovement();
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (!(distance < 10.0F)) return;

        double dx = entity.X - X;
        double dy = entity.Z - Z;
        if (AttackTime == 0)
        {
            EntityArrow arrow = new(World, this);
            double targetHeightOffset = entity.Y + entity.EyeHeight - 0.2F - arrow.Y;
            float distanceFactor = MathHelper.Sqrt(dx * dx + dy * dy) * 0.2F;
            World.Broadcaster.PlaySoundAtEntity(this, "random.bow", 1.0F, 1.0F / (Random.NextFloat() * 0.4F + 0.8F));
            World.SpawnEntity(arrow);
            arrow.SetArrowHeading(dx, targetHeightOffset + distanceFactor, dy, 0.6F, 12.0F);
            AttackTime = 30;
        }

        Yaw = (float)(Math.Atan2(dy, dx) * 180.0D / (float)Math.PI) - 90.0F;
        HasAttacked = true;
    }

    protected override int DropItemId => Item.ARROW.id;

    protected override void DropFewItems()
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
}
