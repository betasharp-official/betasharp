using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;

namespace BetaSharp.Entities;

public abstract class EntityMonster : EntityCreature, Monster
{
    protected int attackStrength = 2;

    public EntityMonster(World world) : base(world)
    {
        health = 20;
    }

    public override void tickMovement()
    {
        float brightness = getBrightnessAtEyes(1.0F);
        if (brightness > 0.5F)
        {
            entityAge += 2;
        }

        base.tickMovement();
    }

    public override void tick()
    {
        base.tick();
        if (!world.isRemote && world.difficulty == 0)
        {
            markDead();
        }

    }

    protected override Entity findPlayerToAttack()
    {
        EntityPlayer player = world.getClosestPlayer(this, 16.0D);
        return player != null && canSee(player) ? player : null;
    }

    public override bool damage(Entity entity, int amount)
    {
        if (base.damage(entity, amount))
        {
            if (passenger != entity && vehicle != entity)
            {
                if (entity != this)
                {
                    playerToAttack = entity;
                }

                return true;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (attackTime <= 0 && distance < 2.0F && entity.boundingBox.MaxY > boundingBox.MinY && entity.boundingBox.MinY < boundingBox.MaxY)
        {
            attackTime = 20;
            entity.damage(this, attackStrength);
        }

    }

    protected override float getBlockPathWeight(int x, int y, int z)
    {
        return 0.5F - world.getLuminance(x, y, z);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
    }

    public override bool canSpawn()
    {
        int x = MathHelper.Floor(base.x);
        int y = MathHelper.Floor(boundingBox.MinY);
        int z = MathHelper.Floor(base.z);
        if (world.getBrightness(LightType.Sky, x, y, z) > random.NextInt(32))
        {
            return false;
        }
        else
        {
            int lightLevel = world.getLightLevel(x, y, z);
            if (world.isThundering())
            {
                int ambientDarkness = world.ambientDarkness;
                world.ambientDarkness = 10;
                lightLevel = world.getLightLevel(x, y, z);
                world.ambientDarkness = ambientDarkness;
            }

            return lightLevel <= random.NextInt(8) && base.canSpawn();
        }
    }
}
