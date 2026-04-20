using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityPig : EntityAnimal
{
    public override EntityType Type => EntityRegistry.Pig;
    public readonly SyncedProperty<bool> Saddled;

    public EntityPig(IWorldContext world) : base(world)
    {
        Texture = "/mob/pig.png";
        SetBoundingBoxSpacing(0.9F, 0.9F);
        Saddled = DataSynchronizer.MakeProperty(16, false);
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetBoolean("Saddle", Saddled.Value);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        Saddled.Value = nbt.GetBoolean("Saddle");
    }

    protected override string getLivingSound()
    {
        return "mob.pig";
    }

    protected override string getHurtSound()
    {
        return "mob.pig";
    }

    protected override string getDeathSound()
    {
        return "mob.pigdeath";
    }

    public override bool Interact(EntityPlayer player)
    {
        if (!Saddled.Value || World.IsRemote || Passenger != null && Passenger != player)
        {
            return false;
        }
        else
        {
            player.SetVehicle(this);
            return true;
        }
    }

    protected override int getDropItemId()
    {
        return FireTicks > 0 ? Item.CookedPorkchop.id : Item.RawPorkchop.id;
    }

    public override void OnStruckByLightning(EntityLightningBolt bolt)
    {
        if (!World.IsRemote)
        {
            EntityPigZombie pigZombie = new EntityPigZombie(World);
            pigZombie.SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, Pitch);
            World.SpawnEntity(pigZombie);
            MarkDead();
        }
    }

    protected override void OnLanding(float fallDistance)
    {
        base.OnLanding(fallDistance);
        if (fallDistance > 5.0F && Passenger is EntityPlayer)
        {
            ((EntityPlayer)Passenger).incrementStat(Achievements.KillPig);
        }
    }
}
