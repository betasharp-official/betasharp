using BetaSharp.Items;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityGhast : EntityFlying, Monster
{
    public override EntityType Type => EntityRegistry.Ghast;
    public readonly SyncedProperty<bool> Charging;
    public int courseChangeCooldown;
    public double waypointX;
    public double waypointY;
    public double waypointZ;
    private Entity targetedEntity;
    private int aggroCooldown;
    public int prevAttackCounter;
    public int attackCounter;

    public EntityGhast(IWorldContext world) : base(world)
    {
        Texture = "/mob/ghast.png";
        SetBoundingBoxSpacing(4.0F, 4.0F);
        IsImmuneToFire = true;
        Charging = DataSynchronizer.MakeProperty<bool>(16, false);
    }

    public override void Tick()
    {
        base.Tick();
        Texture = Charging.Value ? "/mob/ghast_fire.png" : "/mob/ghast.png";
    }

    public override void tickLiving()
    {
        if (!World.IsRemote && World.Difficulty == 0)
        {
            MarkDead();
        }

        func_27021_X();
        prevAttackCounter = attackCounter;
        double dx1 = waypointX - X;
        double dy1 = waypointY - Y;
        double dz1 = waypointZ - Z;
        double distance = (double)MathHelper.Sqrt(dx1 * dx1 + dy1 * dy1 + dz1 * dz1);
        if (distance < 1.0D || distance > 60.0D)
        {
            waypointX = X + (double)((Random.NextFloat() * 2.0F - 1.0F) * 16.0F);
            waypointY = Y + (double)((Random.NextFloat() * 2.0F - 1.0F) * 16.0F);
            waypointZ = Z + (double)((Random.NextFloat() * 2.0F - 1.0F) * 16.0F);
        }

        if (courseChangeCooldown-- <= 0)
        {
            courseChangeCooldown += Random.NextInt(5) + 2;
            if (isCourseTraversable(waypointX, waypointY, waypointZ, distance))
            {
                VelocityX += dx1 / distance * 0.1D;
                VelocityY += dy1 / distance * 0.1D;
                VelocityZ += dz1 / distance * 0.1D;
            }
            else
            {
                waypointX = X;
                waypointY = Y;
                waypointZ = Z;
            }
        }

        if (targetedEntity != null && targetedEntity.Dead)
        {
            targetedEntity = null;
        }

        if (targetedEntity == null || aggroCooldown-- <= 0)
        {
            targetedEntity = World.Entities.GetClosestPlayerTarget(X, Y, Z, 100.0D);
            if (targetedEntity != null)
            {
                aggroCooldown = 20;
            }
        }

        double attackRange = 64.0D;
        if (targetedEntity != null && targetedEntity.GetSquaredDistance(this) < attackRange * attackRange)
        {
            double dx2 = targetedEntity.X - X;
            double dy2 = targetedEntity.BoundingBox.MinY + (double)(targetedEntity.Height / 2.0F) - (Y + (double)(Height / 2.0F));
            double dz2 = targetedEntity.Z - Z;
            BodyYaw = Yaw = -((float)System.Math.Atan2(dx2, dz2)) * 180.0F / (float)System.Math.PI;
            if (canSee(targetedEntity))
            {
                if (attackCounter == 10)
                {
                    World.Broadcaster.PlaySoundAtEntity(this, "mob.ghast.charge", getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
                }

                ++attackCounter;
                if (attackCounter == 20)
                {
                    World.Broadcaster.PlaySoundAtEntity(this, "mob.ghast.fireball", getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
                    EntityFireball fireball = new EntityFireball(World, this, dx2, dy2, dz2);
                    double spawnOffset = 4.0D;
                    Vec3D lookDir = getLook(1.0F);
                    fireball.X = X + lookDir.x * spawnOffset;
                    fireball.Y = Y + (double)(Height / 2.0F) + 0.5D;
                    fireball.Z = Z + lookDir.z * spawnOffset;
                    World.SpawnEntity(fireball);
                    attackCounter = -40;
                }
            }
            else if (attackCounter > 0)
            {
                --attackCounter;
            }
        }
        else
        {
            BodyYaw = Yaw = -((float)System.Math.Atan2(VelocityX, VelocityZ)) * 180.0F / (float)System.Math.PI;
            if (attackCounter > 0)
            {
                --attackCounter;
            }
        }

        if (!World.IsRemote)
        {
            Charging.Value = attackCounter > 10;
        }
    }

    private bool isCourseTraversable(double targetX, double targety, double targetZ, double distance)
    {
        double stepX = (waypointX - X) / distance;
        double stepY = (waypointY - Y) / distance;
        double stepZ = (waypointZ - Z) / distance;
        Box box = BoundingBox;

        for (int i = 1; (double)i < distance; ++i)
        {
            box.Translate(stepX, stepY, stepZ);
            if (World.Entities.GetEntityCollisionsScratch(this, box).Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    protected override String getLivingSound()
    {
        return "mob.ghast.moan";
    }

    protected override String getHurtSound()
    {
        return "mob.ghast.scream";
    }

    protected override String getDeathSound()
    {
        return "mob.ghast.death";
    }

    protected override int getDropItemId()
    {
        return Item.Gunpowder.id;
    }

    protected override float getSoundVolume()
    {
        return 10.0F;
    }

    public override bool canSpawn()
    {
        return Random.NextInt(20) == 0 && base.canSpawn() && World.Difficulty > 0;
    }

    public override int getMaxSpawnedInChunk()
    {
        return 1;
    }
}
