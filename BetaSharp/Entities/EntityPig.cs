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
        texture = "/mob/pig.png";
        setBoundingBoxSpacing(0.9F, 0.9F);
        Saddled = DataSynchronizer.MakeProperty(16, false);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetBoolean("Saddle", Saddled.Value);
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
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

    public override bool interact(EntityPlayer player)
    {
        if (!Saddled.Value || world.IsRemote || passenger != null && passenger != player)
        {
            return false;
        }
        else
        {
            player.setVehicle(this);
            return true;
        }
    }

    protected override int getDropItemId()
    {
        return fireTicks > 0 ? Item.CookedPorkchop.id : Item.RawPorkchop.id;
    }

    public override void onStruckByLightning(EntityLightningBolt bolt)
    {
        if (!world.IsRemote)
        {
            EntityPigZombie pigZombie = new EntityPigZombie(world);
            pigZombie.setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, pitch);
            world.SpawnEntity(pigZombie);
            markDead();
        }
    }

    protected override void onLanding(float fallDistance)
    {
        base.onLanding(fallDistance);
        if (fallDistance > 5.0F && passenger is EntityPlayer)
        {
            ((EntityPlayer)passenger).incrementStat(Achievements.KillPig);
        }
    }
}
