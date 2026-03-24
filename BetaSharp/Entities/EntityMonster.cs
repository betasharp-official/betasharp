using BetaSharp.NBT;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityMonster : EntityCreature, Monster
{
    protected int attackStrength = 2;

    public EntityMonster(IWorldContext world) : base(world)
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
        if (!world.IsRemote && world.Difficulty == 0)
        {
            markDead();
        }

    }

    protected override Entity findPlayerToAttack()
    {
        EntityPlayer player = world.Entities.GetClosestPlayer(this.x, this.y, this.z, 16.0D);
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
        return 0.5F - world.Lighting.GetLuminance(x, y, z);
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
        int x = MathHelper.Floor(this.x);
        int y = MathHelper.Floor(boundingBox.MinY);
        int z = MathHelper.Floor(this.z);
        if (world.Lighting.GetBrightness(LightType.Sky, x, y, z) > random.NextInt(32))
        {
            return false;
        }

        int lightLevel = world.Lighting.GetLightLevel(x, y, z);
        if (world.Environment.IsThundering())
        {
            int ambientDarkness = world.Environment.AmbientDarkness;
            world.Environment.AmbientDarkness = 10;
            lightLevel = world.Lighting.GetLightLevel(x, y, z);
            world.Environment.AmbientDarkness = ambientDarkness;
        }

        return lightLevel <= random.NextInt(8) && base.canSpawn();
    }
}
