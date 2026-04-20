using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.PathFinding;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public class EntityWolf : EntityAnimal
{
    public override EntityType Type => EntityRegistry.Wolf;
    private bool looksWithInterest;
    private float headTiltAmount;
    private float prevHeadTiltAmount;
    private bool isWolfShaking;
    private bool isShaking;
    private float timeWolfIsShaking;
    private float prevTimeWolfIsShaking;

    public readonly SyncedProperty<byte> WolfFlags;
    public readonly SyncedProperty<string> WolfOwner;
    public readonly SyncedProperty<int> WolfHealth;

    public EntityWolf(IWorldContext world) : base(world)
    {
        Texture = "/mob/wolf.png";
        SetBoundingBoxSpacing(0.8F, 0.8F);
        MovementSpeed = 1.1F;
        Health = 8;
        WolfFlags = DataSynchronizer.MakeProperty<byte>(16, 0);
        WolfOwner = DataSynchronizer.MakeProperty<string>(17, "");
        WolfHealth = DataSynchronizer.MakeProperty<int>(18, Health);
    }

    protected override bool BypassesSteppingEffects()
    {
        return false;
    }

    public override string GetTexture()
    {
        return isWolfTamed() ? "/mob/wolf_tame.png" : (isWolfAngry() ? "/mob/wolf_angry.png" : base.GetTexture());
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetBoolean("Angry", isWolfAngry());
        nbt.SetBoolean("Sitting", isWolfSitting());
        if (getWolfOwner() == null)
        {
            nbt.SetString("Owner", "");
        }
        else
        {
            nbt.SetString("Owner", getWolfOwner());
        }

    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        setWolfAngry(nbt.GetBoolean("Angry"));
        setWolfSitting(nbt.GetBoolean("Sitting"));
        string ownerName = nbt.GetString("Owner");
        if (ownerName.Length > 0)
        {
            setWolfOwner(ownerName);
            setWolfTamed(true);
        }

    }

    protected override bool canDespawn()
    {
        return !isWolfTamed();
    }

    protected override string getLivingSound()
    {
        return isWolfAngry() ? "mob.wolf.growl" : (Random.NextInt(3) == 0 ? (isWolfTamed() && WolfHealth.Value < 10 ? "mob.wolf.whine" : "mob.wolf.panting") : "mob.wolf.bark");
    }

    protected override string getHurtSound()
    {
        return "mob.wolf.hurt";
    }

    protected override string getDeathSound()
    {
        return "mob.wolf.death";
    }

    protected override float getSoundVolume()
    {
        return 0.4F;
    }

    protected override int getDropItemId()
    {
        return -1;
    }

    public override void tickLiving()
    {
        base.tickLiving();
        if (!hasAttacked && !hasPath() && isWolfTamed() && Vehicle == null)
        {
            EntityPlayer? owner = World.Entities.Players.Find((player) => player.name.Equals(getWolfOwner(), StringComparison.OrdinalIgnoreCase));
            if (owner != null)
            {
                float distance = owner.GetDistance(this);
                if (distance > 5.0F)
                {
                    getPathOrWalkableBlock(owner, distance);
                }
            }
            else if (!IsInWater())
            {
                setWolfSitting(true);
            }
        }
        else if (playerToAttack == null && !hasPath() && !isWolfTamed() && World.Random.NextInt(100) == 0)
        {
            var nearbySheep = World.Entities.CollectEntitiesOfType<EntitySheep>(new Box(X, Y, Z, X + 1.0D, Y + 1.0D, Z + 1.0D).Expand(16.0D, 4.0D, 16.0D));
            if (nearbySheep.Count > 0)
            {
                setTarget(nearbySheep[World.Random.NextInt(nearbySheep.Count)]);
            }
        }

        if (IsInWater())
        {
            setWolfSitting(false);
        }

        if (!World.IsRemote)
        {
            WolfHealth.Value = Health;
        }

    }

    public override void tickMovement()
    {
        base.tickMovement();
        looksWithInterest = false;
        if (hasCurrentTarget() && !hasPath() && !isWolfAngry())
        {
            Entity currentTarget = getCurrentTarget();
            if (currentTarget is EntityPlayer)
            {
                EntityPlayer targetPlayer = (EntityPlayer)currentTarget;
                ItemStack heldItem = targetPlayer.inventory.GetItemInHand();
                if (heldItem != null)
                {
                    if (!isWolfTamed() && heldItem.ItemId == Item.Bone.id)
                    {
                        looksWithInterest = true;
                    }
                    else if (isWolfTamed() && Item.ITEMS[heldItem.ItemId] is ItemFood)
                    {
                        looksWithInterest = ((ItemFood)Item.ITEMS[heldItem.ItemId]).getIsWolfsFavoriteMeat();
                    }
                }
            }
        }

        if (!InterpolateOnly && isWolfShaking && !isShaking && !hasPath() && OnGround)
        {
            isShaking = true;
            timeWolfIsShaking = 0.0F;
            prevTimeWolfIsShaking = 0.0F;
            World.Broadcaster.EntityEvent(this, (byte)8);
        }

    }

    public override void Tick()
    {
        base.Tick();
        prevHeadTiltAmount = headTiltAmount;
        if (looksWithInterest)
        {
            headTiltAmount += (1.0F - headTiltAmount) * 0.4F;
        }
        else
        {
            headTiltAmount += (0.0F - headTiltAmount) * 0.4F;
        }

        if (looksWithInterest)
        {
            LookTimer = 10;
        }

        if (IsWet())
        {
            isWolfShaking = true;
            isShaking = false;
            timeWolfIsShaking = 0.0F;
            prevTimeWolfIsShaking = 0.0F;
        }
        else if ((isWolfShaking || isShaking) && isShaking)
        {
            if (timeWolfIsShaking == 0.0F)
            {
                World.Broadcaster.PlaySoundAtEntity(this, "mob.wolf.shake", getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
            }

            prevTimeWolfIsShaking = timeWolfIsShaking;
            timeWolfIsShaking += 0.05F;
            if (prevTimeWolfIsShaking >= 2.0F)
            {
                isWolfShaking = false;
                isShaking = false;
                prevTimeWolfIsShaking = 0.0F;
                timeWolfIsShaking = 0.0F;
            }

            if (timeWolfIsShaking > 0.4F)
            {
                float groundY = (float)BoundingBox.MinY;
                int particleCount = (int)(MathHelper.Sin((timeWolfIsShaking - 0.4F) * (float)System.Math.PI) * 7.0F);

                for (int _ = 0; _ < particleCount; ++_)
                {
                    float offsetX = (Random.NextFloat() * 2.0F - 1.0F) * Width * 0.5F;
                    float offsetZ = (Random.NextFloat() * 2.0F - 1.0F) * Width * 0.5F;
                    World.Broadcaster.AddParticle("splash", X + (double)offsetX, (double)(groundY + 0.8F), Z + (double)offsetZ, VelocityX, VelocityY, VelocityZ);
                }
            }
        }

    }

    public bool getWolfShaking()
    {
        return isWolfShaking;
    }

    public float getShadingWhileShaking(float partialTick)
    {
        return 12.0F / 16.0F + (prevTimeWolfIsShaking + (timeWolfIsShaking - prevTimeWolfIsShaking) * partialTick) / 2.0F * 0.25F;
    }

    public float getShakeAngle(float partialTick, float offset)
    {
        float shakeProgress = (prevTimeWolfIsShaking + (timeWolfIsShaking - prevTimeWolfIsShaking) * partialTick + offset) / 1.8F;
        if (shakeProgress < 0.0F)
        {
            shakeProgress = 0.0F;
        }
        else if (shakeProgress > 1.0F)
        {
            shakeProgress = 1.0F;
        }

        return MathHelper.Sin(shakeProgress * (float)System.Math.PI) * MathHelper.Sin(shakeProgress * (float)System.Math.PI * 11.0F) * 0.15F * (float)System.Math.PI;
    }

    public float getInterestedAngle(float partialTick)
    {
        return (prevHeadTiltAmount + (headTiltAmount - prevHeadTiltAmount) * partialTick) * 0.15F * (float)System.Math.PI;
    }

    public override float GetEyeHeight()
    {
        return Height * 0.8F;
    }

    protected override int getMaxFallDistance()
    {
        return isWolfSitting() ? 20 : base.getMaxFallDistance();
    }

    private void getPathOrWalkableBlock(Entity entity, float distanceToOwner)
    {
        PathEntity path = World.Pathing.findPath(this, entity, 16.0F);
        if (path == null && distanceToOwner > 12.0F)
        {
            int ownerBlockX = MathHelper.Floor(entity.X) - 2;
            int ownerBlockY = MathHelper.Floor(entity.Z) - 2;
            int ownerBlockZ = MathHelper.Floor(entity.BoundingBox.MinY);

            for (int dx = 0; dx <= 4; ++dx)
            {
                for (int dy = 0; dy <= 4; ++dy)
                {
                    if ((dx < 1 || dy < 1 || dx > 3 || dy > 3) && World.Reader.ShouldSuffocate(ownerBlockX + dx, ownerBlockZ - 1, ownerBlockY + dy) && !World.Reader.ShouldSuffocate(ownerBlockX + dx, ownerBlockZ, ownerBlockY + dy) && !World.Reader.ShouldSuffocate(ownerBlockX + dx, ownerBlockZ + 1, ownerBlockY + dy))
                    {
                        SetPositionAndAnglesKeepPrevAngles((double)((float)(ownerBlockX + dx) + 0.5F), (double)ownerBlockZ, (double)((float)(ownerBlockY + dy) + 0.5F), Yaw, Pitch);
                        return;
                    }
                }
            }
        }
        else
        {
            setPathToEntity(path);
        }

    }

    protected override bool isMovementCeased()
    {
        return isWolfSitting() || isShaking;
    }

    public override bool Damage(Entity entity, int amount)
    {
        setWolfSitting(false);
        if (entity != null && entity is not EntityPlayer && entity is not EntityArrow)
        {
            amount = (amount + 1) / 2;
        }

        if (!base.Damage((Entity)entity, amount))
        {
            return false;
        }
        else
        {
            if (!isWolfTamed() && !isWolfAngry())
            {
                bool isTargetablePlayer = entity is EntityPlayer { GameMode.CanBeTargeted: true };
                if (isTargetablePlayer)
                {
                    setWolfAngry(true);
                    playerToAttack = entity;
                }

                if (entity is EntityArrow arrow && arrow.owner != null)
                {
                    entity = arrow.owner;
                }

                if (entity is EntityLiving)
                {
                    var nearbyWolves = World.Entities.CollectEntitiesOfType<EntityWolf>(new Box(X, Y, Z, X + 1.0D, Y + 1.0D, Z + 1.0D).Expand(16.0D, 4.0D, 16.0D));

                    foreach (var wolf in nearbyWolves)
                    {
                        if (!wolf.isWolfTamed() && wolf.playerToAttack == null)
                        {
                            wolf.playerToAttack = entity;
                            if (isTargetablePlayer)
                            {
                                wolf.setWolfAngry(true);
                            }
                        }
                    }
                }
            }
            else if (entity != this && entity != null)
            {
                if (isWolfTamed() && entity is EntityPlayer player && !player.GameMode.CanBeTargeted && (player).name.Equals(getWolfOwner(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                playerToAttack = (Entity)entity;
            }

            return true;
        }
    }

    protected override Entity findPlayerToAttack()
    {
        return isWolfAngry() ? World.Entities.GetClosestPlayerTarget(this.X, this.Y, this.Z, 16.0D) : null;
    }

    protected override void attackEntity(Entity entity, float distance)
    {
        if (distance > 2.0F && distance < 6.0F && Random.NextInt(10) == 0)
        {
            if (OnGround)
            {
                double dx = entity.X - X;
                double dy = entity.Z - Z;
                float horizontalDistance = MathHelper.Sqrt(dx * dx + dy * dy);
                VelocityX = dx / (double)horizontalDistance * 0.5D * (double)0.8F + VelocityX * (double)0.2F;
                VelocityZ = dy / (double)horizontalDistance * 0.5D * (double)0.8F + VelocityZ * (double)0.2F;
                VelocityY = (double)0.4F;
            }
        }
        else if ((double)distance < 1.5D && entity.BoundingBox.MaxY > BoundingBox.MinY && entity.BoundingBox.MinY < BoundingBox.MaxY)
        {
            AttackTime = 20;
            byte damageAmount = 2;
            if (isWolfTamed())
            {
                damageAmount = 4;
            }

            entity.Damage(this, damageAmount);
        }

    }

    public override bool Interact(EntityPlayer player)
    {
        ItemStack heldItem = player.inventory.GetItemInHand();
        if (!isWolfTamed())
        {
            if (heldItem != null && heldItem.ItemId == Item.Bone.id && !isWolfAngry())
            {
                heldItem.ConsumeItem(player);
                if (heldItem.Count <= 0)
                {
                    player.inventory.SetStack(player.inventory.SelectedSlot, (ItemStack)null);
                }

                if (!World.IsRemote)
                {
                    if (Random.NextInt(3) == 0)
                    {
                        setWolfTamed(true);
                        setPathToEntity((PathEntity)null);
                        setWolfSitting(true);
                        Health = 20;
                        setWolfOwner(player.name);
                        showHeartsOrSmokeFX(true);
                        World.Broadcaster.EntityEvent(this, 7);
                    }
                    else
                    {
                        showHeartsOrSmokeFX(false);
                        World.Broadcaster.EntityEvent(this, 6);
                    }
                }

                return true;
            }
        }
        else
        {
            if (heldItem != null && Item.ITEMS[heldItem.ItemId] is ItemFood)
            {
                ItemFood food = (ItemFood)Item.ITEMS[heldItem.ItemId];
                if (food.getIsWolfsFavoriteMeat() && WolfHealth.Value < 20)
                {
                    heldItem.ConsumeItem(player);
                    if (heldItem.Count <= 0)
                    {
                        player.inventory.SetStack(player.inventory.SelectedSlot, (ItemStack)null);
                    }

                    heal(((ItemFood)Item.RawPorkchop).getHealAmount());
                    return true;
                }
            }

            if (player.name.Equals(getWolfOwner(), StringComparison.OrdinalIgnoreCase))
            {
                if (!World.IsRemote)
                {
                    setWolfSitting(!isWolfSitting());
                    Jumping = false;
                    setPathToEntity((PathEntity)null);
                }

                return true;
            }
        }

        return false;
    }

    void showHeartsOrSmokeFX(bool showHearts)
    {
        string particleName = "heart";
        if (!showHearts)
        {
            particleName = "smoke";
        }

        for (int _ = 0; _ < 7; ++_)
        {
            double paticleX = Random.NextGaussian() * 0.02D;
            double paticleY = Random.NextGaussian() * 0.02D;
            double paticleZ = Random.NextGaussian() * 0.02D;
            World.Broadcaster.AddParticle(particleName, X + (double)(Random.NextFloat() * Width * 2.0F) - (double)Width, Y + 0.5D + (double)(Random.NextFloat() * Height), Z + (double)(Random.NextFloat() * Width * 2.0F) - (double)Width, paticleX, paticleY, paticleZ);
        }

    }

    public override void ProcessServerEntityStatus(sbyte status)
    {
        if (status == 7)
        {
            showHeartsOrSmokeFX(true);
        }
        else if (status == 6)
        {
            showHeartsOrSmokeFX(false);
        }
        else if (status == 8)
        {
            isShaking = true;
            timeWolfIsShaking = 0.0F;
            prevTimeWolfIsShaking = 0.0F;
        }
        else
        {
            base.ProcessServerEntityStatus(status);
        }

    }

    public float getTailRotation()
    {
        return isWolfAngry() ? (float)System.Math.PI * 0.49F : (isWolfTamed() ? (0.55F - (float)(20 - WolfHealth.Value) * 0.02F) * (float)System.Math.PI : (float)System.Math.PI * 0.2F);
    }

    public override int getMaxSpawnedInChunk()
    {
        return 8;
    }

    public string getWolfOwner()
    {
        return WolfOwner.Value;
    }

    public void setWolfOwner(string name)
    {
        WolfOwner.Value = name;
    }

    public bool isWolfSitting()
    {
        return (WolfFlags.Value & 1) != 0;
    }

    public void setWolfSitting(bool isSitting)
    {
        byte data = WolfFlags.Value;
        if (isSitting)
        {
            WolfFlags.Value = (byte)(data | 1);
        }
        else
        {
            WolfFlags.Value = (byte)(data & -2);
        }
    }

    public bool isWolfAngry()
    {
        return (WolfFlags.Value & 2) != 0;
    }

    public void setWolfAngry(bool isAngry)
    {
        byte data = WolfFlags.Value;
        if (isAngry)
        {
            WolfFlags.Value = (byte)(data | 2);
        }
        else
        {
            WolfFlags.Value = (byte)(data & -3);
        }
    }

    public bool isWolfTamed()
    {
        return (WolfFlags.Value & 4) != 0;
    }

    public void setWolfTamed(bool IsTamed)
    {
        byte data = WolfFlags.Value;
        if (IsTamed)
        {
            WolfFlags.Value = (byte)(data | 4);
        }
        else
        {
            WolfFlags.Value = (byte)(data & -5);
        }

    }
}
