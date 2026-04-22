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
        Texture = "/mob/creeper.png";
        CreeperState = DataSynchronizer.MakeProperty<byte>(16, 255); // -1
        Powered = DataSynchronizer.MakeProperty<bool>(17, false);
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        if (Powered.Value)
        {
            nbt.SetBoolean("powered", true);
        }
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        Powered.Value = nbt.GetBoolean("powered");
    }

    protected override void attackBlockedEntity(Entity entity, float distance)
    {
        if (!World.IsRemote)
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

    public override void Tick()
    {
        lastActiveTime = timeSinceIgnited;
        if (World.IsRemote)
        {
            int state = (sbyte)CreeperState.Value;
            if (state > 0 && timeSinceIgnited == 0)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "random.fuse", 1.0F, 0.5F);
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

        base.Tick();
        if (!World.IsRemote && playerToAttack == null && timeSinceIgnited > 0)
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
            DropItem(Item.RecordThirteen.id + Random.NextInt(2), 1);
        }

    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (!World.IsRemote)
        {
            int state = (sbyte)CreeperState.Value;
            if (state <= 0 && distance < 3.0F || state > 0 && distance < 7.0F)
            {
                if (timeSinceIgnited == 0)
                {
                    World.Broadcaster.PlaySoundAtEntity(this, "random.fuse", 1.0F, 0.5F);
                }

                CreeperState.Value = 1;
                ++timeSinceIgnited;
                if (timeSinceIgnited >= 30)
                {
                    if (Powered.Value)
                    {
                        World.CreateExplosion(this, X, Y, Z, 6.0F);
                    }
                    else
                    {
                        World.CreateExplosion(this, X, Y, Z, 3.0F);
                    }

                    MarkDead();
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

    public override void OnStruckByLightning(EntityLightningBolt bolt)
    {
        base.OnStruckByLightning(bolt);
        Powered.Value = true;
    }
}
