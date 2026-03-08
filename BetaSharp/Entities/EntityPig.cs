using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Entities;

public class EntityPig : EntityAnimal
{
    public EntityPig(World world) : base(world)
    {
        texture = "/mob/pig.png";
        setBoundingBoxSpacing(0.9F, 0.9F);
    }

    protected override void initDataTracker()
    {
        dataWatcher.AddObject(16, (byte)0);
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetBoolean("Saddle", getSaddled());
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        setSaddled(nbt.GetBoolean("Saddle"));
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
        if (!getSaddled() || _ctx.isRemote || passenger != null && passenger != player)
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

    public bool getSaddled()
    {
        return (dataWatcher.getWatchableObjectByte(16) & 1) != 0;
    }

    public void setSaddled(bool isSaddled)
    {
        if (isSaddled)
        {
            dataWatcher.UpdateObject(16, ((byte)1));
        }
        else
        {
            dataWatcher.UpdateObject(16, ((byte)0));
        }

    }

    public override void onStruckByLightning(EntityLightningBolt bolt)
    {
        if (!_ctx.isRemote)
        {
            EntityPigZombie pigZombie = new EntityPigZombie(_ctx);
            pigZombie.setPositionAndAnglesKeepPrevAngles(x, y, z, yaw, pitch);
            _ctx.SpawnEntity(pigZombie);
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
