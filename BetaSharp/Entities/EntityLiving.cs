using BetaSharp.Blocks;
using BetaSharp.Blocks.Materials;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Util.Hit;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityLiving : Entity
{
    public int MaxHealth { get; set; } = 20;
    public float LimbSwingPhase { get; set; }
    public float LimbSwingScale { get; set; }
    public float BodyYaw { get; set; }
    public float LastBodyYaw { get; set; }
    protected float LastWalkProgress { get; set; }
    protected float WalkProgress { get; set; }
    protected float TotalWalkDistance { get; set; }
    protected float LastTotalWalkDistance { get; set; }
    protected bool CanLookAround { get; set; } = true;
    protected string Texture { get; set; } = "/mob/char.png";
    protected float RotationOffset { get; set; } = 0.0F;
    protected string ModelName { get; set; } = null;
    protected float ModelScale { get; set; } = 1.0F;
    protected int ScoreAmount { get; set; } = 0;
    public bool InterpolateOnly { get; set; } = false;
    public float LastSwingAnimationProgress { get; set; }
    public float SwingAnimationProgress { get; set; }
    public int Health { get; set; } = 10;
    public int LastHealth { get; set; }
    private int _livingSoundTime { get; set; }
    public int HurtTime { get; set; }
    public int MaxHurtTime { get; set; }
    public float AttackedAtYaw { get; set; }
    public int DeathTime { get; set; }
    public int AttackTime { get; set; }
    public float CameraPitch { get; set; }
    public float Tilt { get; set; }
    public float LastWalkAnimationSpeed { get; set; }
    public float WalkAnimationSpeed { get; set; }
    public float AnimationPhase { get; set; }
    protected int NewPosRotationIncrements { get; set; }
    protected double NewPosX { get; set; }
    protected double NewPosY { get; set; }
    protected double NewPosZ { get; set; }
    protected double NewRotationYaw { get; set; }
    protected double NewRotationPitch { get; set; }
    protected int DamageForDisplay { get; set; }
    protected int EntityAge { get; set; }
    protected float SidewaysSpeed { get; set; }
    protected float ForwardSpeed { get; set; }
    protected float RotationSpeed { get; set; }
    protected bool Jumping { get; set; }
    protected float DefaultPitch { get; set; } = 0.0F;
    protected float MovementSpeed { get; set; } = 0.7F;
    private Entity _lookTarget;
    protected int LookTimer { get; set; }

    public EntityLiving(IWorldContext world) : base(world)
    {
        PreventEntitySpawning = true;
        LimbSwingScale = (System.Random.Shared.NextSingle() + 1.0f) * 0.01f;
        SetPosition(X, Y, Z);
        LimbSwingPhase = System.Random.Shared.NextSingle() * 12398.0f;
        Yaw = (System.Random.Shared.NextSingle() * (float)Math.PI) * 2.0f;
        StepHeight = 0.5F;
    }

    public virtual void PostSpawn()
    {

    }


    public bool canSee(Entity entity)
    {
        return World.Reader.Raycast(new Vec3D(X, Y + (double)GetEyeHeight(), Z), new Vec3D(entity.X, entity.Y + (double)entity.GetEyeHeight(), entity.Z)).Type == HitResultType.MISS;
    }

    public override string GetTexture()
    {
        return Texture;
    }

    public override bool IsCollidable()
    {
        return !Dead;
    }

    public override bool IsPushable()
    {
        return !Dead;
    }

    public override float GetEyeHeight()
    {
        return Height * 0.85F;
    }

    public virtual int getTalkInterval()
    {
        return 80;
    }

    public void playLivingSound()
    {
        string sound = getLivingSound();
        if (sound != null)
        {
            World.Broadcaster.PlaySoundAtEntity(this, sound, getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
        }

    }

    public override void BaseTick()
    {
        LastSwingAnimationProgress = SwingAnimationProgress;
        base.BaseTick();
        if (Random.NextInt(1000) < _livingSoundTime++)
        {
            _livingSoundTime = -getTalkInterval();
            playLivingSound();
        }

        if (IsAlive() && IsInsideWall())
        {
            Damage(null, 1);
        }

        if (IsImmuneToFire || World.IsRemote)
        {
            FireTicks = 0;
        }

        int i;
        if (IsAlive() && IsInFluid(Material.Water) && !canBreatheUnderwater())
        {
            --Air;
            if (Air == -20)
            {
                Air = 0;

                for (i = 0; i < 8; ++i)
                {
                    float offsetX = Random.NextFloat() - Random.NextFloat();
                    float offsetY = Random.NextFloat() - Random.NextFloat();
                    float offsetZ = Random.NextFloat() - Random.NextFloat();
                    World.Broadcaster.AddParticle("bubble", X + (double)offsetX, Y + (double)offsetY, Z + (double)offsetZ, VelocityX, VelocityY, VelocityZ);
                }

                Damage(null, 2);
            }

            FireTicks = 0;
        }
        else
        {
            Air = MaxAir;
        }

        CameraPitch = Tilt;
        if (AttackTime > 0)
        {
            --AttackTime;
        }

        if (HurtTime > 0)
        {
            --HurtTime;
        }

        if (Hearts > 0)
        {
            --Hearts;
        }

        if (Health <= 0)
        {
            ++DeathTime;
            if (DeathTime > 20)
            {
                onEntityDeath();
                MarkDead();

                for (i = 0; i < 20; ++i)
                {
                    double velX = Random.NextGaussian() * 0.02D;
                    double velY = Random.NextGaussian() * 0.02D;
                    double velZ = Random.NextGaussian() * 0.02D;
                    World.Broadcaster.AddParticle("explode", X + (double)(Random.NextFloat() * Width * 2.0F) - (double)Width, Y + (double)(Random.NextFloat() * Height), Z + (double)(Random.NextFloat() * Width * 2.0F) - (double)Width, velX, velY, velZ);
                }
            }
        }

        LastTotalWalkDistance = TotalWalkDistance;
        LastBodyYaw = BodyYaw;
        PrevYaw = Yaw;
        PrevPitch = Pitch;
    }

    public override void Move(double x, double y, double z)
    {
        if (!InterpolateOnly/* || this is ClientPlayerEntity*/) base.Move(x, y, z);
    }

    public void animateSpawn()
    {
        for (int i = 0; i < 20; ++i)
        {
            double velX = Random.NextGaussian() * 0.02D;
            double velY = Random.NextGaussian() * 0.02D;
            double velZ = Random.NextGaussian() * 0.02D;
            double spread = 10.0D;
            World.Broadcaster.AddParticle("explode", X + (double)(Random.NextFloat() * Width * 2.0F) - (double)Width - velX * spread, Y + (double)(Random.NextFloat() * Height) - velY * spread, Z + (double)(Random.NextFloat() * Width * 2.0F) - (double)Width - velZ * spread, velX, velY, velZ);
        }

    }

    public override void TickRiding()
    {
        base.TickRiding();
        LastWalkProgress = WalkProgress;
        WalkProgress = 0.0F;
    }

    public override void SetPositionAndAnglesAvoidEntities(double newPosX, double newPosY, double newPosZ, float newRotationYaw, float newRotationPitch, int newPosRotationIncrements)
    {
        StandingEyeHeight = 0.0F;
        this.NewPosX = newPosX;
        this.NewPosY = newPosY;
        this.NewPosZ = newPosZ;
        this.NewRotationYaw = (double)newRotationYaw;
        this.NewRotationPitch = (double)newRotationPitch;
        this.NewPosRotationIncrements = newPosRotationIncrements;
    }

    public override void Tick()
    {
        base.Tick();
        tickMovement();
        double dx = X - PrevX;
        double dz = Z - PrevZ;
        float horizontalDistance = MathHelper.Sqrt(dx * dx + dz * dz);
        float computedYaw = BodyYaw;
        float walkSpeed = 0.0F;
        LastWalkProgress = WalkProgress;
        float walkAmount = 0.0F;
        if (horizontalDistance > 0.05F)
        {
            walkAmount = 1.0F;
            walkSpeed = horizontalDistance * 3.0F;
            computedYaw = (float)System.Math.Atan2(dz, dx) * 180.0F / (float)System.Math.PI - 90.0F;
        }

        if (SwingAnimationProgress > 0.0F)
        {
            computedYaw = base.Yaw;
        }

        if (!OnGround)
        {
            walkAmount = 0.0F;
        }

        WalkProgress += (walkAmount - WalkProgress) * 0.3F;

        float yawDelta;
        for (yawDelta = computedYaw - BodyYaw; yawDelta < -180.0F; yawDelta += 360.0F)
        {
        }

        while (yawDelta >= 180.0F)
        {
            yawDelta -= 360.0F;
        }

        BodyYaw += yawDelta * 0.3F;

        float headYawDelta;
        for (headYawDelta = base.Yaw - BodyYaw; headYawDelta < -180.0F; headYawDelta += 360.0F)
        {
        }

        while (headYawDelta >= 180.0F)
        {
            headYawDelta -= 360.0F;
        }

        bool headFacingBackward = headYawDelta < -90.0F || headYawDelta >= 90.0F;
        if (headYawDelta < -75.0F)
        {
            headYawDelta = -75.0F;
        }

        if (headYawDelta >= 75.0F)
        {
            headYawDelta = 75.0F;
        }

        BodyYaw = base.Yaw - headYawDelta;
        if (headYawDelta * headYawDelta > 2500.0F)
        {
            BodyYaw += headYawDelta * 0.2F;
        }

        if (headFacingBackward)
        {
            walkSpeed *= -1.0F;
        }

        while (base.Yaw - PrevYaw < -180.0F)
        {
            PrevYaw -= 360.0F;
        }

        while (base.Yaw - PrevYaw >= 180.0F)
        {
            PrevYaw += 360.0F;
        }

        while (BodyYaw - LastBodyYaw < -180.0F)
        {
            LastBodyYaw -= 360.0F;
        }

        while (BodyYaw - LastBodyYaw >= 180.0F)
        {
            LastBodyYaw += 360.0F;
        }

        while (Pitch - PrevPitch < -180.0F)
        {
            PrevPitch -= 360.0F;
        }

        while (Pitch - PrevPitch >= 180.0F)
        {
            PrevPitch += 360.0F;
        }

        TotalWalkDistance += walkSpeed;
    }

    protected override void SetBoundingBoxSpacing(float widthOffset, float heightOffset)
    {
        base.SetBoundingBoxSpacing(widthOffset, heightOffset);
    }

    public virtual void heal(int amount)
    {
        if (Health > 0)
        {
            Health += amount;
            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }

            Hearts = MaxHealth / 2;
        }
    }

    public override bool Damage(Entity? entity, int amount)
    {
        if (World.IsRemote)
        {
            return false;
        }
        else
        {
            EntityAge = 0;
            if (Health <= 0)
            {
                return false;
            }
            else
            {
                WalkAnimationSpeed = 1.5F;
                bool playHurtEffects = true;
                if ((float)Hearts > (float)MaxHealth / 2.0F)
                {
                    if (amount <= DamageForDisplay)
                    {
                        return false;
                    }

                    applyDamage(amount - DamageForDisplay);
                    DamageForDisplay = amount;
                    playHurtEffects = false;
                }
                else
                {
                    DamageForDisplay = amount;
                    LastHealth = Health;
                    Hearts = MaxHealth;
                    applyDamage(amount);
                    HurtTime = MaxHurtTime = 10;
                }

                AttackedAtYaw = 0.0F;
                if (playHurtEffects)
                {
                    World.Broadcaster.EntityEvent(this, (byte)2);
                    ScheduleVelocityUpdate();
                    if (entity != null)
                    {
                        double knockbackX = entity.X - X;

                        double knockbackZ;
                        for (knockbackZ = entity.Z - Z; knockbackX * knockbackX + knockbackZ * knockbackZ < 1.0E-4D; knockbackZ = (System.Random.Shared.NextDouble() - System.Random.Shared.NextDouble()) * 0.01D)
                        {
                            knockbackX = (System.Random.Shared.NextDouble() - System.Random.Shared.NextDouble()) * 0.01D;
                        }

                        AttackedAtYaw = (float)(System.Math.Atan2(knockbackZ, knockbackX) * 180.0D / (double)((float)System.Math.PI)) - Yaw;
                        knockBack(entity, amount, knockbackX, knockbackZ);
                    }
                    else
                    {
                        AttackedAtYaw = (float)((int)(System.Random.Shared.NextDouble() * 2.0D) * 180);
                    }
                }

                if (Health <= 0)
                {
                    if (playHurtEffects)
                    {
                        World.Broadcaster.PlaySoundAtEntity(this, getDeathSound(), getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
                    }

                    onKilledBy(entity);
                }
                else if (playHurtEffects)
                {
                    World.Broadcaster.PlaySoundAtEntity(this, getHurtSound(), getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
                }

                return true;
            }
        }
    }

    public override void AnimateHurt()
    {
        HurtTime = MaxHurtTime = 10;
        AttackedAtYaw = 0.0F;
    }

    protected virtual void applyDamage(int amount)
    {
        Health -= amount;
    }

    protected virtual float getSoundVolume()
    {
        return 1.0F;
    }

    protected virtual string getLivingSound()
    {
        return null;
    }

    protected virtual string getHurtSound()
    {
        return "random.hurt";
    }

    protected virtual string getDeathSound()
    {
        return "random.hurt";
    }

    public void knockBack(Entity entity, int amount, double dx, double dy)
    {
        float knockbackLength = MathHelper.Sqrt(dx * dx + dy * dy);
        float knockbackStrength = 0.4F;
        VelocityX /= 2.0D;
        VelocityY /= 2.0D;
        VelocityZ /= 2.0D;
        VelocityX -= dx / (double)knockbackLength * (double)knockbackStrength;
        VelocityY += (double)0.4F;
        VelocityZ -= dy / (double)knockbackLength * (double)knockbackStrength;
        if (VelocityY > (double)0.4F)
        {
            VelocityY = (double)0.4F;
        }

    }

    public virtual void onKilledBy(Entity entity)
    {
        if (ScoreAmount >= 0 && entity != null)
        {
            entity.UpdateKilledAchievement(this, ScoreAmount);
        }

        if (entity != null)
        {
            entity.OnKillOther(this);
        }

        if (!World.IsRemote)
        {
            dropFewItems();
        }

        World.Broadcaster.EntityEvent(this, (byte)3);
    }

    protected virtual void dropFewItems()
    {
        int dropItemId = getDropItemId();
        if (dropItemId > 0)
        {
            int dropCount = Random.NextInt(3);

            for (int dropIndex = 0; dropIndex < dropCount; ++dropIndex)
            {
                DropItem(dropItemId, 1);
            }
        }

    }

    protected virtual int getDropItemId()
    {
        return 0;
    }

    protected override void OnLanding(float fallDistance)
    {
        base.OnLanding(fallDistance);
        int fallDamage = (int)Math.Ceiling((double)(fallDistance - 3.0F));
        if (fallDamage > 0)
        {
            Damage(null, fallDamage);
            int groundBlockId = World.Reader.GetBlockId(MathHelper.Floor(X), MathHelper.Floor(Y - (double)0.2F - (double)StandingEyeHeight), MathHelper.Floor(Z));
            if (groundBlockId > 0)
            {
                BlockSoundGroup soundGroup = Block.Blocks[groundBlockId].SoundGroup;
                World.Broadcaster.PlaySoundAtEntity(this, soundGroup.StepSound, soundGroup.Volume * 0.5F, soundGroup.Pitch * (12.0F / 16.0F));
            }
        }

    }

    protected virtual float AirSpeed() => 0.02f;

    public virtual void travel(float strafe, float forward)
    {
        double previousY;
        if (IsInWater())
        {
            previousY = Y;
            MoveNonSolid(strafe, forward, 0.02F);
            Move(VelocityX, VelocityY, VelocityZ);
            VelocityX *= (double)0.8F;
            VelocityY *= (double)0.8F;
            VelocityZ *= (double)0.8F;
            VelocityY -= 0.02D;
            if (HorizontalCollison && GetEntitiesInside(VelocityX, VelocityY + (double)0.6F - Y + previousY, VelocityZ))
            {
                VelocityY = (double)0.3F;
            }
        }
        else if (IsTouchingLava())
        {
            previousY = Y;
            MoveNonSolid(strafe, forward, 0.02F);
            Move(VelocityX, VelocityY, VelocityZ);
            VelocityX *= 0.5D;
            VelocityY *= 0.5D;
            VelocityZ *= 0.5D;
            VelocityY -= 0.02D;
            if (HorizontalCollison && GetEntitiesInside(VelocityX, VelocityY + (double)0.6F - Y + previousY, VelocityZ))
            {
                VelocityY = (double)0.3F;
            }
        }
        else
        {
            float friction = 0.91F;
            if (OnGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = World.Reader.GetBlockId(MathHelper.Floor(X), MathHelper.Floor(BoundingBox.MinY) - 1, MathHelper.Floor(Z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].Slipperiness * 0.91F;
                }
            }

            float movementFactor = 0.16277136F / (friction * friction * friction);
            MoveNonSolid(strafe, forward, OnGround ? 0.1F * movementFactor : AirSpeed());
            friction = 0.91F;
            if (OnGround)
            {
                friction = 546.0F * 0.1F * 0.1F * 0.1F;
                int groundBlockId = World.Reader.GetBlockId(MathHelper.Floor(X), MathHelper.Floor(BoundingBox.MinY) - 1, MathHelper.Floor(Z));
                if (groundBlockId > 0)
                {
                    friction = Block.Blocks[groundBlockId].Slipperiness * 0.91F;
                }
            }

            if (isOnLadder())
            {
                float ladderSpeedClamp = 0.15F;
                if (VelocityX < (double)(-ladderSpeedClamp))
                {
                    VelocityX = (double)(-ladderSpeedClamp);
                }

                if (VelocityX > (double)ladderSpeedClamp)
                {
                    VelocityX = (double)ladderSpeedClamp;
                }

                if (VelocityZ < (double)(-ladderSpeedClamp))
                {
                    VelocityZ = (double)(-ladderSpeedClamp);
                }

                if (VelocityZ > (double)ladderSpeedClamp)
                {
                    VelocityZ = (double)ladderSpeedClamp;
                }

                FallDistance = 0.0F;
                if (VelocityY < -0.15D)
                {
                    VelocityY = -0.15D;
                }

                if (IsSneaking() && VelocityY < 0.0D)
                {
                    VelocityY = 0.0D;
                }
            }

            Move(VelocityX, VelocityY, VelocityZ);
            if (HorizontalCollison && isOnLadder())
            {
                VelocityY = 0.2D;
            }

            VelocityY -= 0.08D;
            VelocityY *= (double)0.98F;
            VelocityX *= (double)friction;
            VelocityZ *= (double)friction;
        }

        LastWalkAnimationSpeed = WalkAnimationSpeed;
        previousY = X - PrevX;
        double deltaZ = Z - PrevZ;
        float distanceMoved = MathHelper.Sqrt(previousY * previousY + deltaZ * deltaZ) * 4.0F;
        if (distanceMoved > 1.0F)
        {
            distanceMoved = 1.0F;
        }

        WalkAnimationSpeed += (distanceMoved - WalkAnimationSpeed) * 0.4F;
        AnimationPhase += WalkAnimationSpeed;
    }

    public virtual bool isOnLadder()
    {
        int x = MathHelper.Floor(base.X);
        int y = MathHelper.Floor(BoundingBox.MinY);
        int z = MathHelper.Floor(base.Z);
        return World.Reader.GetBlockId(x, y, z) == Block.Ladder.id;
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        nbt.SetShort("Health", (short)Health);
        nbt.SetShort("HurtTime", (short)HurtTime);
        nbt.SetShort("DeathTime", (short)DeathTime);
        nbt.SetShort("AttackTime", (short)AttackTime);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        Health = nbt.GetShort("Health");
        if (!nbt.HasKey("Health"))
        {
            Health = 10;
        }

        HurtTime = nbt.GetShort("HurtTime");
        DeathTime = nbt.GetShort("DeathTime");
        AttackTime = nbt.GetShort("AttackTime");
    }

    public override bool IsAlive()
    {
        return !Dead && Health > 0;
    }

    public virtual bool canBreatheUnderwater()
    {
        return false;
    }

    public virtual void tickMovement()
    {
        if (World.IsRemote && this is not EntityPlayer)
        {
            int minChunkX = MathHelper.Floor(BoundingBox.MinX) >> 4;
            int maxChunkX = MathHelper.Floor(BoundingBox.MaxX) >> 4;
            int minChunkZ = MathHelper.Floor(BoundingBox.MinZ) >> 4;
            int maxChunkZ = MathHelper.Floor(BoundingBox.MaxZ) >> 4;

            for (int chunkX = minChunkX; chunkX <= maxChunkX; ++chunkX)
            {
                for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; ++chunkZ)
                {
                    var chunk = World.ChunkHost.GetChunk(chunkX, chunkZ);
                    if (!chunk.Loaded)
                    {
                        Jumping = false;
                        SidewaysSpeed = 0.0F;
                        ForwardSpeed = 0.0F;
                        RotationSpeed = 0.0F;
                        VelocityX = VelocityY = VelocityZ = 0.0D;
                        return;
                    }
                }
            }
        }

        if (NewPosRotationIncrements > 0)
        {
            double newX = X + (NewPosX - X) / (double)NewPosRotationIncrements;
            double newY = Y + (NewPosY - Y) / (double)NewPosRotationIncrements;
            double newZ = Z + (NewPosZ - Z) / (double)NewPosRotationIncrements;

            double yawDelta;
            for (yawDelta = NewRotationYaw - (double)Yaw; yawDelta < -180.0D; yawDelta += 360.0D)
            {
            }

            while (yawDelta >= 180.0D)
            {
                yawDelta -= 360.0D;
            }

            Yaw = (float)(Yaw + yawDelta / NewPosRotationIncrements);
            Pitch = (float)(Pitch + (NewRotationPitch - Pitch) / NewPosRotationIncrements);
            --NewPosRotationIncrements;
            SetPosition(newX, newY, newZ);
            SetRotation(Yaw, Pitch);
            var collisions = World.Entities.GetEntityCollisionsScratch(this, BoundingBox.Contract(1.0D / 32.0D, 0.0D, 1.0D / 32.0D));
            if (collisions.Count > 0)
            {
                double highestCollisionY = BoundingBox.MinY;
                bool applyStep = false;

                foreach (var col in collisions)
                {
                    if (col.MaxY > highestCollisionY && col.MaxY <= BoundingBox.MinY + 1.0)
                    {
                        highestCollisionY = col.MaxY;
                        applyStep = true;
                    }
                }

                if (applyStep)
                {
                    newY += highestCollisionY - BoundingBox.MinY;
                }

                SetPosition(newX, newY, newZ);
            }
        }

        if (isMovementBlocked())
        {
            Jumping = false;
            SidewaysSpeed = 0.0F;
            ForwardSpeed = 0.0F;
            RotationSpeed = 0.0F;
        }
        else if (!InterpolateOnly)
        {
            tickLiving();
        }

        bool isInWater = base.IsInWater();
        bool isTouchingLava = base.IsTouchingLava();
        if (Jumping)
        {
            if (isInWater)
            {
                VelocityY += (double)0.04F;
            }
            else if (isTouchingLava)
            {
                VelocityY += (double)0.04F;
            }
            else if (OnGround)
            {
                jump();
            }
        }

        SidewaysSpeed *= 0.98F;
        ForwardSpeed *= 0.98F;
        RotationSpeed *= 0.9F;
        travel(SidewaysSpeed, ForwardSpeed);
        var nearbyEntities = World.Entities.GetEntitiesScratch(this, BoundingBox.Expand((double)0.2F, 0.0D, (double)0.2F));
        if (nearbyEntities != null && nearbyEntities.Count > 0)
        {
            for (int i = 0; i < nearbyEntities.Count; ++i)
            {
                Entity entity = nearbyEntities[i];
                if (entity.IsPushable())
                {
                    entity.OnCollision(this);
                }
            }
        }

    }

    protected virtual bool isMovementBlocked()
    {
        return Health <= 0;
    }

    protected virtual void jump()
    {
        VelocityY = (double)0.42F;
    }

    protected virtual bool canDespawn()
    {
        return true;
    }

    protected void func_27021_X()
    {
        EntityPlayer player = World.Entities.GetClosestPlayer(X, Y, Z, -1.0D);
        if (canDespawn() && player != null)
        {
            double dx = player.X - X;
            double dy = player.Y - Y;
            double dz = player.Z - Z;
            double squaredDistance = dx * dx + dy * dy + dz * dz;
            if (squaredDistance > 16384.0D)
            {
                MarkDead();
            }

            if (EntityAge > 600 && Random.NextInt(800) == 0)
            {
                if (squaredDistance < 1024.0D)
                {
                    EntityAge = 0;
                }
                else
                {
                    MarkDead();
                }
            }
        }

    }

    public virtual void tickLiving()
    {
        ++EntityAge;
        func_27021_X();
        SidewaysSpeed = 0.0F;
        ForwardSpeed = 0.0F;
        const float lookRange = 8.0F;
        if (Random.NextFloat() < 0.02F)
        {
            EntityPlayer? closestPlayer = World.Entities.GetClosestPlayer(X, Y, Z, (double)lookRange);
            if (closestPlayer != null)
            {
                _lookTarget = closestPlayer;
                LookTimer = 10 + Random.NextInt(20);
            }
            else
            {
                RotationSpeed = (Random.NextFloat() - 0.5F) * 20.0F;
            }
        }

        if (_lookTarget != null)
        {
            faceEntity(_lookTarget, 10.0F, (float)getMaxFallDistance());
            if (LookTimer-- <= 0 || _lookTarget.Dead || _lookTarget.GetSquaredDistance(this) > (double)(lookRange * lookRange))
            {
                _lookTarget = null;
            }
        }
        else
        {
            if (Random.NextFloat() < 0.05F)
            {
                RotationSpeed = (Random.NextFloat() - 0.5F) * 20.0F;
            }

            Yaw += RotationSpeed;
            Pitch = DefaultPitch;
        }

        bool isInWater = base.IsInWater();
        bool isTouchingLava = base.IsTouchingLava();
        if (isInWater || isTouchingLava)
        {
            Jumping = Random.NextFloat() < 0.8F;
        }

    }

    protected virtual int getMaxFallDistance()
    {
        return 40;
    }

    public void faceEntity(Entity entity, float yawSpeed, float pitchSpeed)
    {
        double dx = entity.X - X;
        double dz = entity.Z - Z;
        double dy;
        if (entity is EntityLiving)
        {
            EntityLiving ent = (EntityLiving)entity;
            dy = Y + (double)GetEyeHeight() - (ent.Y + (double)ent.GetEyeHeight());
        }
        else
        {
            dy = (entity.BoundingBox.MinY + entity.BoundingBox.MaxY) / 2.0D - (Y + (double)GetEyeHeight());
        }

        double horizontalDistance = (double)MathHelper.Sqrt(dx * dx + dz * dz);
        float targetYaw = (float)(System.Math.Atan2(dz, dx) * 180.0D / (double)((float)System.Math.PI)) - 90.0F;
        float targetPitch = (float)(-(System.Math.Atan2(dy, horizontalDistance) * 180.0D / (double)((float)System.Math.PI)));
        Pitch = -updateRotation(Pitch, targetPitch, pitchSpeed);
        Yaw = updateRotation(Yaw, targetYaw, yawSpeed);
    }

    public bool hasCurrentTarget()
    {
        return _lookTarget != null;
    }

    public Entity getCurrentTarget()
    {
        return _lookTarget;
    }

    private static float updateRotation(float currentRotation, float targetRotation, float maxDelta)
    {
        float delta;
        for (delta = targetRotation - currentRotation; delta < -180.0F; delta += 360.0F)
        {
        }

        while (delta >= 180.0F)
        {
            delta -= 360.0F;
        }

        if (delta > maxDelta)
        {
            delta = maxDelta;
        }

        if (delta < -maxDelta)
        {
            delta = -maxDelta;
        }

        return currentRotation + delta;
    }

    public static void onEntityDeath()
    {
    }

    public virtual bool canSpawn()
    {
        return World.Entities.CanSpawnEntity(BoundingBox) && World.Entities.GetEntityCollisionsScratch(this, BoundingBox).Count == 0 && !World.Reader.IsMaterialInBox(BoundingBox, m => m.IsFluid);
    }

    protected override void TickInVoid()
    {
        Damage(null, 4);
    }

    public float getSwingProgress(float partialTick)
    {
        float progressDelta = SwingAnimationProgress - LastSwingAnimationProgress;
        if (progressDelta < 0.0F)
        {
            ++progressDelta;
        }

        return LastSwingAnimationProgress + progressDelta * partialTick;
    }

    public Vec3D GetPosition()
    {
        return new Vec3D(X, Y, Z);
    }

    public Vec3D GetPosition(float partialTick)
    {
        if (partialTick == 1.0F)
        {
            return new Vec3D(X, Y, Z);
        }
        else
        {
            double x = PrevX + (base.X - PrevX) * (double)partialTick;
            double y = PrevY + (base.Y - PrevY) * (double)partialTick;
            double z = PrevZ + (base.Z - PrevZ) * (double)partialTick;
            return new Vec3D(x, y, z);
        }
    }

    public override Vec3D? GetLookVector()
    {
        return getLook(1.0F);
    }

    public Vec3D getLook(float partialTick)
    {
        float cosYaw;
        float sinYaw;
        float cosPitch;
        float sinPitch;
        if (partialTick == 1.0F)
        {
            cosYaw = MathHelper.Cos(-Yaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            sinYaw = MathHelper.Sin(-Yaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            cosPitch = -MathHelper.Cos(-Pitch * ((float)System.Math.PI / 180.0F));
            sinPitch = MathHelper.Sin(-Pitch * ((float)System.Math.PI / 180.0F));
            return new Vec3D((double)(sinYaw * cosPitch), (double)sinPitch, (double)(cosYaw * cosPitch));
        }
        else
        {
            cosYaw = PrevPitch + (Pitch - PrevPitch) * partialTick;
            sinYaw = PrevYaw + (Yaw - PrevYaw) * partialTick;
            cosPitch = MathHelper.Cos(-sinYaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            sinPitch = MathHelper.Sin(-sinYaw * ((float)System.Math.PI / 180.0F) - (float)System.Math.PI);
            float horizontalCos = -MathHelper.Cos(-cosYaw * ((float)System.Math.PI / 180.0F));
            float verticalSin = MathHelper.Sin(-cosYaw * ((float)System.Math.PI / 180.0F));
            return new Vec3D((double)(sinPitch * horizontalCos), (double)verticalSin, (double)(cosPitch * horizontalCos));
        }
    }

    public HitResult rayTrace(double range, float partialTick)
    {
        Vec3D startPos = GetPosition(partialTick);
        Vec3D lookDir = getLook(partialTick);
        Vec3D endPos = startPos + range * lookDir;
        return World.Reader.Raycast(startPos, endPos);
    }

    public virtual int getMaxSpawnedInChunk()
    {
        return 4;
    }

    public virtual ItemStack getHeldItem()
    {
        return null;
    }

    public override void ProcessServerEntityStatus(sbyte statusId)
    {
        if (statusId == 2)
        {
            WalkAnimationSpeed = 1.5F;
            Hearts = MaxHealth;
            HurtTime = MaxHurtTime = 10;
            AttackedAtYaw = 0.0F;
            World.Broadcaster.PlaySoundAtEntity(this, getHurtSound(), getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
            Damage(null, 0);
        }
        else if (statusId == 3)
        {
            World.Broadcaster.PlaySoundAtEntity(this, getDeathSound(), getSoundVolume(), (Random.NextFloat() - Random.NextFloat()) * 0.2F + 1.0F);
            Health = 0;
            onKilledBy(null);
        }
        else
        {
            base.ProcessServerEntityStatus(statusId);
        }

    }

    public virtual bool isSleeping()
    {
        return false;
    }

    public virtual int getItemStackTextureId(ItemStack item)
    {
        return item.getTextureId();
    }
}
