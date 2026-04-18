using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityPig : EntityAnimal
{
    public readonly SyncedProperty<bool> Saddled;

    public EntityPig(IWorldContext world) : base(world)
    {
        Texture = "/mob/pig.png";
        SetBoundingBoxSpacing(0.9F, 0.9F);
        Saddled = DataSynchronizer.MakeProperty(16, false);
    }

    public override EntityType Type => EntityRegistry.Pig;

    protected override string? LivingSound => "mob.pig";

    protected override string? HurtSound => "mob.pig";

    protected override string? DeathSound => "mob.pigdeath";

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetBoolean("Saddle", Saddled.Value);
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        Saddled.Value = nbt.GetBoolean("Saddle");
    }

    public override bool Interact(EntityPlayer player)
    {
        if (!Saddled.Value || World.IsRemote || (Passenger != null && !Equals(Passenger, player))) return false;

        player.SetVehicle(this);
        return true;
    }

    protected override int DropItemId => FireTicks > 0 ? Item.CookedPorkchop.id : Item.RawPorkchop.id;

    public override void OnStruckByLightning(EntityLightningBolt bolt)
    {
        if (World.IsRemote) return;

        EntityPigZombie pigZombie = new(World);
        pigZombie.SetPositionAndAnglesKeepPrevAngles(X, Y, Z, Yaw, Pitch);
        World.SpawnEntity(pigZombie);
        MarkDead();
    }

    protected override void OnLanding(float fallDistance)
    {
        base.OnLanding(fallDistance);
        if (fallDistance > 5.0F && Passenger is EntityPlayer player)
        {
            player.IncrementStat(Achievements.KillPig);
        }
    }
}
