using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.PathFinding;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityWolf : EntityAnimal
{
    private readonly SyncedProperty<byte> _wolfFlags;
    private readonly SyncedProperty<int> _wolfHealth;
    private readonly SyncedProperty<string?> _wolfOwner;
    private float _headTiltAmount;
    private bool _isShaking;
    private bool _isWolfShaking;
    private bool _looksWithInterest;
    private float _prevHeadTiltAmount;
    private float _prevTimeWolfIsShaking;
    private float _timeWolfIsShaking;

    public EntityWolf(IWorldContext world) : base(world)
    {
        Texture = "/mob/wolf.png";
        SetBoundingBoxSpacing(0.8F, 0.8F);
        MovementSpeed = 1.1F;
        Health = 8;
        _wolfFlags = DataSynchronizer.MakeProperty<byte>(16, 0);
        _wolfOwner = DataSynchronizer.MakeProperty<string?>(17, "");
        _wolfHealth = DataSynchronizer.MakeProperty(18, Health);
    }

    public override EntityType Type => EntityRegistry.Wolf;

    protected override bool CanDespawn => !IsWolfTamed;

    protected override string LivingSound => IsWolfAngry ? "mob.wolf.growl" : Random.NextInt(3) == 0 ? IsWolfTamed && _wolfHealth.Value < 10 ? "mob.wolf.whine" : "mob.wolf.panting" : "mob.wolf.bark";

    protected override string? HurtSound => "mob.wolf.hurt";

    protected override string? DeathSound => "mob.wolf.death";

    protected override float SoundVolume => 0.4F;

    protected override int DropItemId => -1;

    public override float EyeHeight => Height * 0.8F;

    protected override bool IsMovementCeased => IsWolfSitting || _isShaking;

    public override int MaxSpawnedInChunk => 8;

    public string? WolfOwner
    {
        get => _wolfOwner.Value;
        set => _wolfOwner.Value = value;
    }


    public bool IsWolfSitting
    {
        get => (_wolfFlags.Value & 1) != 0;
        set
        {
            if (value)
                _wolfFlags.Value |= 1;
            else
                _wolfFlags.Value &= unchecked((byte)~1);
        }
    }


    public bool IsWolfAngry
    {
        get => (_wolfFlags.Value & 2) != 0;
        private set
        {
            if (value)
                _wolfFlags.Value |= 2;
            else
                _wolfFlags.Value &= unchecked((byte)~2);
        }
    }


    public bool IsWolfTamed
    {
        get => (_wolfFlags.Value & 4) != 0;
        private set
        {
            if (value)
                _wolfFlags.Value |= 4;
            else
                _wolfFlags.Value &= unchecked((byte)~4);
        }
    }

    protected sealed override void SetBoundingBoxSpacing(float widthOffset, float heightOffset) => base.SetBoundingBoxSpacing(widthOffset, heightOffset);

    protected override bool BypassesSteppingEffects() => false;

    public override string GetTexture() => IsWolfTamed ? "/mob/wolf_tame.png" : IsWolfAngry ? "/mob/wolf_angry.png" : base.GetTexture();

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetBoolean("Angry", IsWolfAngry);
        nbt.SetBoolean("Sitting", IsWolfSitting);
        nbt.SetString("Owner", WolfOwner ?? "");
    }

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        IsWolfAngry = nbt.GetBoolean("Angry");
        IsWolfSitting = nbt.GetBoolean("Sitting");
        string ownerName = nbt.GetString("Owner");
        if (ownerName.Length <= 0) return;
        WolfOwner = ownerName;
        IsWolfTamed = true;
    }

    protected override void TickLiving()
    {
        base.TickLiving();
        if (!HasAttacked && !HasPath && IsWolfTamed && Vehicle == null)
        {
            EntityPlayer? owner = World.Entities.Players.Find(player => player.Name != null && player.Name.Equals(WolfOwner, StringComparison.OrdinalIgnoreCase));
            if (owner != null)
            {
                float distance = owner.GetDistance(this);
                if (distance > 5.0F)
                {
                    getPathOrWalkableBlock(owner, distance);
                }
            }
            else if (!IsInWater)
            {
                IsWolfSitting = true;
            }
        }
        else if (Target == null && !HasPath && !IsWolfTamed && World.Random.NextInt(100) == 0)
        {
            List<EntitySheep> nearbySheep = World.Entities.CollectEntitiesOfType<EntitySheep>(new Box(X, Y, Z, X + 1.0D, Y + 1.0D, Z + 1.0D).Expand(16.0D, 4.0D, 16.0D));
            if (nearbySheep.Count > 0)
            {
                Target = nearbySheep[World.Random.NextInt(nearbySheep.Count)];
            }
        }

        if (IsInWater) IsWolfSitting = false;
        if (!World.IsRemote) _wolfHealth.Value = Health;
    }

    protected override void TickMovement()
    {
        base.TickMovement();
        _looksWithInterest = false;
        if (HasCurrentTarget && !HasPath && !IsWolfAngry)
        {
            if (CurrentTarget is EntityPlayer targetPlayer)
            {
                ItemStack? heldItem = targetPlayer.Inventory.ItemInHand;
                if (heldItem != null)
                {
                    _looksWithInterest = IsWolfTamed switch
                    {
                        false when heldItem.ItemId == Item.Bone.id => true,
                        true when Item.ITEMS[heldItem.ItemId] is ItemFood => ((ItemFood)Item.ITEMS[heldItem.ItemId]!).getIsWolfsFavoriteMeat(),
                        _ => _looksWithInterest
                    };
                }
            }
        }

        if (InterpolateOnly || !_isWolfShaking || _isShaking || HasPath || !OnGround)
        {
            return;
        }

        _isShaking = true;
        _timeWolfIsShaking = 0.0F;
        _prevTimeWolfIsShaking = 0.0F;
        World.Broadcaster.EntityEvent(this, 8);
    }

    public override void Tick()
    {
        base.Tick();
        _prevHeadTiltAmount = _headTiltAmount;
        if (_looksWithInterest)
        {
            _headTiltAmount += (1.0F - _headTiltAmount) * 0.4F;
        }
        else
        {
            _headTiltAmount += (0.0F - _headTiltAmount) * 0.4F;
        }

        if (_looksWithInterest)
        {
            LookTimer = 10;
        }

        if (IsWet)
        {
            _isWolfShaking = true;
            _isShaking = false;
            _timeWolfIsShaking = 0.0F;
            _prevTimeWolfIsShaking = 0.0F;
        }
        else if ((_isWolfShaking || _isShaking) && _isShaking)
        {
            if (_timeWolfIsShaking == 0.0F)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "mob.wolf.shake", SoundVolume, (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
            }

            _prevTimeWolfIsShaking = _timeWolfIsShaking;
            _timeWolfIsShaking += 0.05F;
            if (_prevTimeWolfIsShaking >= 2.0F)
            {
                _isWolfShaking = false;
                _isShaking = false;
                _prevTimeWolfIsShaking = 0.0F;
                _timeWolfIsShaking = 0.0F;
            }

            if (_timeWolfIsShaking > 0.4F)
            {
                float groundY = (float)BoundingBox.MinY;
                int particleCount = (int)(MathHelper.Sin((_timeWolfIsShaking - 0.4F) * (float)Math.PI) * 7.0F);

                for (int _ = 0; _ < particleCount; ++_)
                {
                    float offsetX = (Random.NextFloat() * 2.0F - 1.0F) * Width * 0.5F;
                    float offsetZ = (Random.NextFloat() * 2.0F - 1.0F) * Width * 0.5F;
                    World.Broadcaster.AddParticle("splash", X + offsetX, groundY + 0.8F, Z + offsetZ, VelocityX, VelocityY, VelocityZ);
                }
            }
        }
    }

    public bool getWolfShaking() => _isWolfShaking;

    public float getShadingWhileShaking(float partialTick) => 12.0F / 16.0F + (_prevTimeWolfIsShaking + (_timeWolfIsShaking - _prevTimeWolfIsShaking) * partialTick) / 2.0F * 0.25F;

    public float getShakeAngle(float partialTick, float offset)
    {
        float shakeProgress = (_prevTimeWolfIsShaking + (_timeWolfIsShaking - _prevTimeWolfIsShaking) * partialTick + offset) / 1.8F;
        shakeProgress = shakeProgress switch
        {
            < 0.0F => 0.0F,
            > 1.0F => 1.0F,
            _ => shakeProgress
        };

        return MathHelper.Sin(shakeProgress * (float)Math.PI) * MathHelper.Sin(shakeProgress * (float)Math.PI * 11.0F) * 0.15F * (float)Math.PI;
    }

    public float getInterestedAngle(float partialTick) => (_prevHeadTiltAmount + (_headTiltAmount - _prevHeadTiltAmount) * partialTick) * 0.15F * (float)Math.PI;

    protected override int getMaxFallDistance() => IsWolfSitting ? 20 : base.getMaxFallDistance();

    private void getPathOrWalkableBlock(Entity entity, float distanceToOwner)
    {
        PathEntity? path = World.Pathing.findPath(this, entity, 16.0F);
        if (path == null && distanceToOwner > 12.0F)
        {
            int ownerBlockX = MathHelper.Floor(entity.X) - 2;
            int ownerBlockY = MathHelper.Floor(entity.Z) - 2;
            int ownerBlockZ = MathHelper.Floor(entity.BoundingBox.MinY);

            for (int dx = 0; dx <= 4; ++dx)
            {
                for (int dy = 0; dy <= 4; ++dy)
                {
                    if ((dx >= 1 && dy >= 1 && dx <= 3 && dy <= 3) || !World.Reader.ShouldSuffocate(ownerBlockX + dx, ownerBlockZ - 1, ownerBlockY + dy) || World.Reader.ShouldSuffocate(ownerBlockX + dx, ownerBlockZ, ownerBlockY + dy) ||
                        World.Reader.ShouldSuffocate(ownerBlockX + dx, ownerBlockZ + 1, ownerBlockY + dy))
                    {
                        continue;
                    }

                    SetPositionAndAnglesKeepPrevAngles(ownerBlockX + dx + 0.5F, ownerBlockZ, ownerBlockY + dy + 0.5F, Yaw, Pitch);
                    return;
                }
            }
        }
        else
        {
            setPathToEntity(path);
        }
    }

    public override bool Damage(Entity? entity, int amount)
    {
        IsWolfSitting = false;
        if (entity != null && entity is not EntityPlayer && entity is not EntityArrow)
        {
            amount = (amount + 1) / 2;
        }

        if (!base.Damage(entity, amount)) return false;

        if (!IsWolfTamed && !IsWolfAngry)
        {
            bool isTargetablePlayer = entity is EntityPlayer { GameMode.CanBeTargeted: true };
            if (isTargetablePlayer)
            {
                IsWolfAngry = true;
                Target = entity;
            }

            if (entity is EntityArrow arrow)
            {
                entity = arrow.Owner;
            }

            if (entity is not EntityLiving) return true;

            List<EntityWolf> nearbyWolves = World.Entities.CollectEntitiesOfType<EntityWolf>(new Box(X, Y, Z, X + 1.0D, Y + 1.0D, Z + 1.0D).Expand(16.0D, 4.0D, 16.0D));

            foreach (EntityWolf wolf in nearbyWolves)
            {
                if (wolf is not { IsWolfTamed: false, Target: null })
                {
                    continue;
                }

                wolf.Target = entity;
                if (isTargetablePlayer)
                {
                    wolf.IsWolfAngry = true;
                }
            }
        }
        else if (!Equals(entity, this) && entity != null)
        {
            if (IsWolfTamed && entity is EntityPlayer { GameMode.CanBeTargeted: false } player && player.Name != null && player.Name.Equals(WolfOwner, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Target = entity;
        }

        return true;
    }

    protected override Entity? FindPlayerToAttack() => IsWolfAngry ? World.Entities.GetClosestPlayerTarget(X, Y, Z, 16.0D) : null;

    protected override void attackEntity(Entity entity, float distance)
    {
        if (distance is > 2.0F and < 6.0F && Random.NextInt(10) == 0)
        {
            if (!OnGround) return;

            double dx = entity.X - X;
            double dy = entity.Z - Z;
            float horizontalDistance = MathHelper.Sqrt(dx * dx + dy * dy);
            VelocityX = dx / horizontalDistance * 0.5D * 0.8F + VelocityX * 0.2F;
            VelocityZ = dy / horizontalDistance * 0.5D * 0.8F + VelocityZ * 0.2F;
            VelocityY = 0.4F;
        }
        else if (distance < 1.5D && entity.BoundingBox.MaxY > BoundingBox.MinY && entity.BoundingBox.MinY < BoundingBox.MaxY)
        {
            AttackTime = 20;
            byte damageAmount = 2;
            if (IsWolfTamed)
            {
                damageAmount = 4;
            }

            entity.Damage(this, damageAmount);
        }
    }

    public override bool Interact(EntityPlayer player)
    {
        ItemStack? heldItem = player.Inventory.ItemInHand;
        if (!IsWolfTamed)
        {
            if (heldItem == null || heldItem.ItemId != Item.Bone.id || IsWolfAngry) return false;

            heldItem.ConsumeItem(player);
            if (heldItem.Count <= 0)
            {
                player.Inventory.SetStack(player.Inventory.SelectedSlot, null);
            }

            if (World.IsRemote) return true;

            if (Random.NextInt(3) == 0)
            {
                IsWolfTamed = true;
                setPathToEntity(null);
                IsWolfSitting = true;
                Health = 20;
                WolfOwner = player.Name;
                ShowHeartsOrSmokeFx(true);
                World.Broadcaster.EntityEvent(this, 7);
            }
            else
            {
                ShowHeartsOrSmokeFx(false);
                World.Broadcaster.EntityEvent(this, 6);
            }
        }
        else
        {
            if (heldItem != null && Item.ITEMS[heldItem.ItemId] is ItemFood)
            {
                ItemFood? food = (ItemFood?)Item.ITEMS[heldItem.ItemId];
                if (food != null && food.getIsWolfsFavoriteMeat() && _wolfHealth.Value < 20)
                {
                    heldItem.ConsumeItem(player);
                    if (heldItem.Count <= 0)
                    {
                        player.Inventory.SetStack(player.Inventory.SelectedSlot, null);
                    }

                    Heal(((ItemFood)Item.RawPorkchop).getHealAmount());
                    return true;
                }
            }

            if (player.Name != null && !player.Name.Equals(WolfOwner, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (World.IsRemote)
            {
                return true;
            }

            IsWolfSitting = !IsWolfSitting;
            Jumping = false;
            setPathToEntity(null);
        }

        return true;
    }

    private void ShowHeartsOrSmokeFx(bool showHearts)
    {
        string particleName = "heart";
        if (!showHearts)
        {
            particleName = "smoke";
        }

        for (int _ = 0; _ < 7; ++_)
        {
            double particleX = Random.NextGaussian() * 0.02D;
            double particleY = Random.NextGaussian() * 0.02D;
            double particleZ = Random.NextGaussian() * 0.02D;
            World.Broadcaster.AddParticle(particleName, X + Random.NextFloat() * Width * 2.0F - Width, Y + 0.5D + Random.NextFloat() * Height, Z + Random.NextFloat() * Width * 2.0F - Width, particleX, particleY, particleZ);
        }
    }

    public override void ProcessServerEntityStatus(sbyte status)
    {
        switch (status)
        {
            case 7:
                ShowHeartsOrSmokeFx(true);
                break;
            case 6:
                ShowHeartsOrSmokeFx(false);
                break;
            case 8:
                _isShaking = true;
                _timeWolfIsShaking = 0.0F;
                _prevTimeWolfIsShaking = 0.0F;
                break;
            default:
                base.ProcessServerEntityStatus(status);
                break;
        }
    }

    public float GetTailRotation() => IsWolfAngry ? (float)Math.PI * 0.49F : IsWolfTamed ? (0.55F - (20 - _wolfHealth.Value) * 0.02F) * (float)Math.PI : (float)Math.PI * 0.2F;
}
