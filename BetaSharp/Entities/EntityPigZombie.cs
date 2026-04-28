using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

internal class EntityPigZombie : EntityZombie
{
    private static readonly ItemStack s_defaultHeldItem = new(Item.GoldenSword, 1);
    private int _angerLevel;
    private int _randomSoundDelay;

    public EntityPigZombie(IWorldContext world) : base(world)
    {
        Texture = "/mob/pigzombie.png";
        MovementSpeed = 0.5F;
        AttackStrength = 5;
        IsImmuneToFire = true;
    }

    public override EntityType Type => EntityRegistry.PigZombie;

    protected override string? LivingSound => "mob.zombiepig.zpig";

    protected override string? HurtSound => "mob.zombiepig.zpighurt";

    protected override string? DeathSound => "mob.zombiepig.zpigdeath";

    public override ItemStack HeldItem => s_defaultHeldItem;

    public override void Tick()
    {
        MovementSpeed = Target != null ? 0.95F : 0.5F;
        if (_randomSoundDelay > 0 && --_randomSoundDelay == 0)
        {
            World.Broadcaster.PlaySoundAtEntity(this, "mob.zombiepig.zpigangry", SoundVolume * 2.0F, ((Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F) * 1.8F);
        }

        base.Tick();
    }

    public override bool CanSpawn() => World.Difficulty > 0 && World.Entities.CanSpawnEntity(BoundingBox) && World.Entities.GetEntityCollisionsScratch(this, BoundingBox).Count == 0 &&
                                       !World.Reader.IsMaterialInBox(BoundingBox, m => m.IsFluid);

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetShort("Anger", (short)_angerLevel);
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        _angerLevel = nbt.GetShort("Anger");
    }

    protected override Entity? FindPlayerToAttack() => _angerLevel == 0 ? null : base.FindPlayerToAttack();

    public override bool Damage(Entity? entity, int amount)
    {
        if (entity is not EntityPlayer { GameMode.CanBeTargeted: true })
        {
            return base.Damage(entity, amount);
        }

        List<Entity> entities = World.Entities.GetEntities(this, BoundingBox.Expand(32.0D, 32.0D, 32.0D));

        foreach (Entity inBoundEntity in entities)
        {
            if (inBoundEntity is EntityPigZombie pigZombie)
            {
                pigZombie.becomeAngryAt(entity);
            }
        }

        becomeAngryAt(entity);

        return base.Damage(entity, amount);
    }

    private void becomeAngryAt(Entity entity)
    {
        Target = entity;
        _angerLevel = 400 + Random.NextInt(400);
        _randomSoundDelay = Random.NextInt(40);
    }

    protected override int DropItemId => Item.CookedPorkchop.id;
}
