using BetaSharp.Items;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntitySpider : EntityMonster
{
    private const double ViewDistance = 16.0D;

    public EntitySpider(IWorldContext world) : base(world)
    {
        Texture = "/mob/spider.png";
        SetBoundingBoxSpacing(1.4F, 0.9F);
        MovementSpeed = 0.8F;
    }

    public override EntityType Type => EntityRegistry.Spider;

    protected override double PassengerRidingHeight => Height * 0.75D - 0.5D;

    protected override string? LivingSound => "mob.spider";

    protected override string? HurtSound => "mob.spider";

    protected override string? DeathSound => "mob.spiderdeath";

    protected override int DropItemId => Item.String.id;

    protected override bool IsOnLadder => HorizontalCollision;

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    public override void PostSpawn()
    {
        if (World.Random.NextInt(100) != 0) return;

        EntitySkeleton skeleton = new(World);
        skeleton.SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, 0.0F);
        World.SpawnEntity(skeleton);
        skeleton.SetVehicle(this);
    }

    protected override bool BypassesSteppingEffects() => false;

    protected override Entity? FindPlayerToAttack()
    {
        float brightness = GetBrightnessAtEyes(1.0F);
        return !(brightness < 0.5F) ? null : World.Entities.GetClosestPlayerTarget(X, Y, Z, ViewDistance);
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        float brightness = GetBrightnessAtEyes(1.0F);
        if (brightness > 0.5F && Random.NextInt(100) == 0)
        {
            Target = null;
        }
        else
        {
            if (distance is > 2.0F and < 6.0F && Random.NextInt(10) == 0)
            {
                if (!OnGround) return;

                double dx = entity.X - X;
                double dz = entity.Z - Z;
                float horizontalDistance = MathHelper.Sqrt(dx * dx + dz * dz);
                VelocityX = dx / horizontalDistance * 0.5D * 0.8F + VelocityX * 0.2F;
                VelocityZ = dz / horizontalDistance * 0.5D * 0.8F + VelocityZ * 0.2F;
                VelocityY = 0.4F;
            }
            else
            {
                base.attackEntity(entity, distance);
            }
        }
    }
}
