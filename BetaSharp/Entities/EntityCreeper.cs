using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Entities;

public class EntityCreeper : EntityMonster
{
    int timeSinceIgnited;
    int lastActiveTime;


    public EntityCreeper(World world) : base(world)
    {
        texture = "/mob/creeper.png";
    }

    protected override void initDataTracker()
    {
        base.initDataTracker();
        dataWatcher.AddObject(16, (byte)255); // -1
        dataWatcher.AddObject(17, (byte)0);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        if (dataWatcher.getWatchableObjectByte(17) == 1)
        {
            nbt.SetBoolean("powered", true);
        }

    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        dataWatcher.UpdateObject(17,(byte)(nbt.GetBoolean("powered") ? 1 : 0));
    }

    protected override void attackBlockedEntity(Entity entity, float distance)
    {
        if (!world.IsRemote)
        {
            if (timeSinceIgnited > 0)
            {
                setCreeperState(-1);
                --timeSinceIgnited;
                if (timeSinceIgnited < 0)
                {
                    timeSinceIgnited = 0;
                }
            }

        }
    }

    public override void tick()
    {
        lastActiveTime = timeSinceIgnited;
        if (world.IsRemote)
        {
            int state = getCreeperState();
            if (state > 0 && timeSinceIgnited == 0)
            {
                world.playSound(this, "random.fuse", 1.0F, 0.5F);
            }

            timeSinceIgnited += state;
            if (timeSinceIgnited < 0)
            {
                timeSinceIgnited = 0;
            }

            if (timeSinceIgnited >= 30)
            {
                timeSinceIgnited = 30;
            }
        }

        base.tick();
        if (playerToAttack == null && timeSinceIgnited > 0)
        {
            setCreeperState(-1);
            --timeSinceIgnited;
            if (timeSinceIgnited < 0)
            {
                timeSinceIgnited = 0;
            }
        }

    }

    protected override string getHurtSound()
    {
        return "mob.creeper";
    }

    protected override string getDeathSound()
    {
        return "mob.creeperdeath";
    }

    public override void onKilledBy(Entity entity)
    {
        base.onKilledBy(entity);
        if (entity is EntitySkeleton)
        {
            dropItem(Item.RecordThirteen.id + random.NextInt(2), 1);
        }

    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (!world.IsRemote)
        {
            int state = getCreeperState();
            if (state <= 0 && distance < 3.0F || state > 0 && distance < 7.0F)
            {
                if (timeSinceIgnited == 0)
                {
                    world.playSound(this, "random.fuse", 1.0F, 0.5F);
                }

                setCreeperState(1);
                ++timeSinceIgnited;
                if (timeSinceIgnited >= 30)
                {
                    if (getPowered())
                    {
                        world.createExplosion(this, x, y, z, 6.0F);
                    }
                    else
                    {
                        world.createExplosion(this, x, y, z, 3.0F);
                    }

                    markDead();
                }

                hasAttacked = true;
            }
            else
            {
                setCreeperState(-1);
                --timeSinceIgnited;
                if (timeSinceIgnited < 0)
                {
                    timeSinceIgnited = 0;
                }
            }

        }
    }

    public bool getPowered()
    {
        return dataWatcher.getWatchableObjectByte(17) == 1;
    }

    public float setCreeperFlashTime(float partialTick)
    {
        return ((float)lastActiveTime + (float)(timeSinceIgnited - lastActiveTime) * partialTick) / 28.0F;
    }

    protected override int getDropItemId()
    {
        return Item.Gunpowder.id;
    }

    private int getCreeperState()
    {
        return dataWatcher.getWatchableObjectByte(16);
    }

    private void setCreeperState(int state)
    {
        dataWatcher.UpdateObject(16, (byte)state);
    }

    public override void onStruckByLightning(EntityLightningBolt bolt)
    {
        base.onStruckByLightning(bolt);
        dataWatcher.UpdateObject(17, (byte)(1));
    }
}
