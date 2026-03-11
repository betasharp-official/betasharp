using BetaSharp.Items;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public class EntityGhast : EntityFlying, Monster
{
    public readonly SyncedProperty<bool> Charging;
    public int courseChangeCooldown;
    public double waypointX;
    public double waypointY;
    public double waypointZ;
    private Entity targetedEntity;
    private int aggroCooldown;
    public int prevAttackCounter;
    public int attackCounter;

    public EntityGhast(World world) : base(world)
    {
        texture = "/mob/ghast.png";
        setBoundingBoxSpacing(4.0F, 4.0F);
        isImmuneToFire = true;
        Charging = DataSynchronizer.MakeProperty<bool>(16, false);
    }

    public override void tick()
    {
        base.tick();
        texture = Charging.Value ? "/mob/ghast_fire.png" : "/mob/ghast.png";
    }

    public override void tickLiving()
    {
        if (!world.isRemote && world.difficulty == 0)
        {
            markDead();
        }

        func_27021_X();
        prevAttackCounter = attackCounter;
        double dx1 = waypointX - x;
        double dy1 = waypointY - y;
        double dz1 = waypointZ - z;
        double distance = (double)MathHelper.Sqrt(dx1 * dx1 + dy1 * dy1 + dz1 * dz1);
        if (distance < 1.0D || distance > 60.0D)
        {
            waypointX = x + (double)((random.NextFloat() * 2.0F - 1.0F) * 16.0F);
            waypointY = y + (double)((random.NextFloat() * 2.0F - 1.0F) * 16.0F);
            waypointZ = z + (double)((random.NextFloat() * 2.0F - 1.0F) * 16.0F);
        }

        if (courseChangeCooldown-- <= 0)
        {
            courseChangeCooldown += random.NextInt(5) + 2;
            if (isCourseTraversable(waypointX, waypointY, waypointZ, distance))
            {
                velocityX += dx1 / distance * 0.1D;
                velocityY += dy1 / distance * 0.1D;
                velocityZ += dz1 / distance * 0.1D;
            }
            else
            {
                waypointX = x;
                waypointY = y;
                waypointZ = z;
            }
        }

        if (targetedEntity != null && targetedEntity.dead)
        {
            targetedEntity = null;
        }

        if (targetedEntity == null || aggroCooldown-- <= 0)
        {
            targetedEntity = world.getClosestPlayer(this, 100.0D);
            if (targetedEntity != null)
            {
                aggroCooldown = 20;
            }
        }

        double attackRange = 64.0D;
        if (targetedEntity != null && targetedEntity.getSquaredDistance(this) < attackRange * attackRange)
        {
            double dx2 = targetedEntity.x - x;
            double dy2 = targetedEntity.boundingBox.MinY + (double)(targetedEntity.height / 2.0F) - (y + (double)(height / 2.0F));
            double dz2 = targetedEntity.z - z;
            bodyYaw = yaw = -((float)System.Math.Atan2(dx2, dz2)) * 180.0F / (float)System.Math.PI;
            if (canSee(targetedEntity))
            {
                if (attackCounter == 10)
                {
                    world.playSound(this, "mob.ghast.charge", getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
                }

                ++attackCounter;
                if (attackCounter == 20)
                {
                    world.playSound(this, "mob.ghast.fireball", getSoundVolume(), (random.NextFloat() - random.NextFloat()) * 0.2F + 1.0F);
                    EntityFireball fireball = new EntityFireball(world, this, dx2, dy2, dz2);
                    double spawnOffset = 4.0D;
                    Vec3D lookDir = getLook(1.0F);
                    fireball.x = x + lookDir.x * spawnOffset;
                    fireball.y = y + (double)(height / 2.0F) + 0.5D;
                    fireball.z = z + lookDir.z * spawnOffset;
                    world.SpawnEntity(fireball);
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
            bodyYaw = yaw = -((float)System.Math.Atan2(velocityX, velocityZ)) * 180.0F / (float)System.Math.PI;
            if (attackCounter > 0)
            {
                --attackCounter;
            }
        }

        if (!world.isRemote)
        {
            Charging.Value = attackCounter > 10;
        }
    }

    private bool isCourseTraversable(double targetX, double targety, double targetZ, double distance)
    {
        double stepX = (waypointX - x) / distance;
        double stepY = (waypointY - y) / distance;
        double stepZ = (waypointZ - z) / distance;
        Box box = boundingBox;

        for (int i = 1; (double)i < distance; ++i)
        {
            box.Translate(stepX, stepY, stepZ);
            if (world.GetEntityCollisions(this, box).Count > 0)
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
        return random.NextInt(20) == 0 && base.canSpawn() && world.difficulty > 0;
    }

    public override int getMaxSpawnedInChunk()
    {
        return 1;
    }
}
