using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityCreeper : EntityMonster
{
    public override EntityType Type => EntityRegistry.Creeper;
    public readonly SyncedProperty<byte> CreeperState;
    public readonly SyncedProperty<bool> Powered;
    private int timeSinceIgnited;
    private int lastActiveTime;

    public EntityCreeper(IWorldContext world) : base(world)
    {
        texture = "/mob/creeper.png";
        CreeperState = DataSynchronizer.MakeProperty<byte>(16, 255); // -1
        Powered = DataSynchronizer.MakeProperty<bool>(17, false);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        if (Powered.Value)
        {
            nbt.SetBoolean("powered", true);
        }
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        Powered.Value = nbt.GetBoolean("powered");
    }

    protected override void attackBlockedEntity(Entity entity, float distance)
    {
        if (!world.IsRemote)
        {
            if (timeSinceIgnited > 0)
            {
                CreeperState.Value = 255;
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
            int state = (sbyte)CreeperState.Value;
            if (state > 0 && timeSinceIgnited == 0) {
                world.Broadcaster.PlaySoundAtEntity(this, "random.fuse", 1.0F, 0.5F);
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
        if (!world.IsRemote && playerToAttack == null && timeSinceIgnited > 0)
        {
            CreeperState.Value = 255;
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
            int state = (sbyte)CreeperState.Value;
            if (state <= 0 && distance < 3.0F || state > 0 && distance < 7.0F)
            {
                if (timeSinceIgnited == 0)
                {
                    world.Broadcaster.PlaySoundAtEntity(this, "random.fuse", 1.0F, 0.5F);
                }

                CreeperState.Value = 1;
                ++timeSinceIgnited;
                if (timeSinceIgnited >= 30)
                {
                    if (Powered.Value)
                    {
                        world.CreateExplosion(this, x, y, z, 6.0F);
                    }
                    else
                    {
                        world.CreateExplosion(this, x, y, z, 3.0F);
                    }

                    markDead();
                }

                hasAttacked = true;
            }
            else
            {
                CreeperState.Value = 255;
                --timeSinceIgnited;
                if (timeSinceIgnited < 0)
                {
                    timeSinceIgnited = 0;
                }
            }

        }
    }

    public float GetCreeperFlashTime(float partialTick)
    {
        return ((float)lastActiveTime + (float)(timeSinceIgnited - lastActiveTime) * partialTick) / 28.0F;
    }

    protected override int getDropItemId()
    {
        return Item.Gunpowder.id;
    }

    public override void onStruckByLightning(EntityLightningBolt bolt)
    {
        base.onStruckByLightning(bolt);
        Powered.Value = true;
    }
}
