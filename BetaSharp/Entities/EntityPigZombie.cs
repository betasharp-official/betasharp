using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

internal class EntityPigZombie : EntityZombie
{
    public override EntityType Type => EntityRegistry.PigZombie;
    private int angerLevel;
    private int randomSoundDelay;
    private static readonly ItemStack defaultHeldItem = new ItemStack(Item.GoldenSword, 1);

    public EntityPigZombie(IWorldContext world) : base(world)
    {
        Texture = "/mob/pigzombie.png";
        MovementSpeed = 0.5F;
        attackStrength = 5;
        IsImmuneToFire = true;
    }

    public override void Tick()
    {
        MovementSpeed = playerToAttack != null ? 0.95F : 0.5F;
        if (randomSoundDelay > 0 && --randomSoundDelay == 0)
        {
            World.Broadcaster.PlaySoundAtEntity(this, "mob.zombiepig.zpigangry", getSoundVolume() * 2.0F, ((Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F) * 1.8F);
        }

        base.Tick();
    }

    public override bool canSpawn()
    {
        return World.Difficulty > 0 && World.Entities.CanSpawnEntity(BoundingBox) && World.Entities.GetEntityCollisionsScratch(this, BoundingBox).Count == 0 && !World.Reader.IsMaterialInBox(BoundingBox, m => m.IsFluid);
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetShort("Anger", (short)angerLevel);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        angerLevel = nbt.GetShort("Anger");
    }

    protected override Entity findPlayerToAttack()
    {
        return angerLevel == 0 ? null : base.findPlayerToAttack();
    }

    public override void tickMovement()
    {
        base.tickMovement();
    }

    public override bool Damage(Entity entity, int amount)
    {
        if (entity is EntityPlayer { GameMode.CanBeTargeted: true })
        {
            var entities = World.Entities.GetEntities(this, BoundingBox.Expand(32.0D, 32.0D, 32.0D));

            for (int i = 0; i < entities.Count; ++i)
            {
                Entity inBoundEntity = entities[i];
                if (inBoundEntity is EntityPigZombie)
                {
                    EntityPigZombie pigZombie = (EntityPigZombie)inBoundEntity;
                    pigZombie.becomeAngryAt(entity);
                }
            }

            becomeAngryAt(entity);
        }

        return base.Damage(entity, amount);
    }

    private void becomeAngryAt(Entity entity)
    {
        playerToAttack = entity;
        angerLevel = 400 + Random.NextInt(400);
        randomSoundDelay = Random.NextInt(40);
    }

    protected override String getLivingSound()
    {
        return "mob.zombiepig.zpig";
    }

    protected override String getHurtSound()
    {
        return "mob.zombiepig.zpighurt";
    }

    protected override String getDeathSound()
    {
        return "mob.zombiepig.zpigdeath";
    }

    protected override int getDropItemId()
    {
        return Item.CookedPorkchop.id;
    }

    public override ItemStack getHeldItem()
    {
        return defaultHeldItem;
    }
}
