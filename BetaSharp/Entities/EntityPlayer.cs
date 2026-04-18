using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Blocks.Materials;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.NBT;
using BetaSharp.Registries;
using BetaSharp.Screens;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Entities;

public abstract class EntityPlayer : EntityLiving
{
    protected const float AirFlySpeedMult = 5f;
    public readonly InventoryPlayer Inventory;
    public readonly ScreenHandler PlayerScreenHandler;
    private int _damageSpill;
    private Vec3i? _playerSpawnCoordinate;
    private int _sleepTimer;
    private Vec3i? _startMinecartRidingCoordinate;
    public Vec3D CapePos;
    public Vec3D PrevCapePos;
    public float ChangeDimensionCooldown;
    public ScreenHandler? CurrentScreenHandler;
    public int DimensionId;
    public EntityFish? FishHook = null;
    protected bool HandSwinging;
    protected int HandSwingTicks;
    protected bool InTeleportationState;
    public float LastScreenDistortion;
    public string? Name;
    public string? PlayerCloakUrl;
    protected int PortalCooldown = 20;
    public float PrevStepBobbingAmount;
    protected int Score;
    protected bool Sleeping;
    public Vec3i? SleepingPos;
    public float SleepOffsetX;
    public float SleepOffsetY;
    public float SleepOffsetZ;
    public float StepBobbingAmount;

    protected EntityPlayer(IWorldContext world) : base(world)
    {
        Inventory = new InventoryPlayer(this);
        PlayerScreenHandler = new PlayerScreenHandler(Inventory, !world.IsRemote);
        CurrentScreenHandler = PlayerScreenHandler;
        StandingEyeHeight = 1.62F - 0.5F;
        Vec3i spawnPos = world.Properties.GetSpawnPos();
        SetPositionAndAnglesKeepPrevAngles(spawnPos.X + 0.5D, spawnPos.Y + 1, spawnPos.Z + 0.5D, 0.0F, 0.0F);
        Health = 20;
        ModelName = "humanoid";
        RotationOffset = 180.0F;
        FireImmunityTicks = 20;
        Texture = "/mob/char.png";
    }

    /// <summary>
    ///     The player's current game mode value. All reads go through the holder, so if the
    ///     server resyncs registry data the value updates in-place without requiring a
    ///     separate update packet.
    /// </summary>
    public GameMode GameMode => GameModeHolder.Value;

    /// <summary>
    ///     The holder backing <see cref="GameMode" />.
    /// </summary>
    public Holder<GameMode> GameModeHolder { get; set; } = new(new GameMode());

    public override float EyeHeight => 0.12F;

    public override bool IsSleeping => Sleeping;

    protected new float AirSpeed => GameMode.DisallowFlying ? 0.02f : AirFlySpeedMult * 0.02f;

    protected override bool canBreatheUnderwater() => !GameMode.NeedsAir;

    protected void TickSleep()
    {
        if (IsSleeping)
        {
            ++_sleepTimer;
            if (_sleepTimer > 100)
            {
                _sleepTimer = 100;
            }

            if (World.IsRemote)
            {
                return;
            }

            if (!IsSleepingInBed())
            {
                WakeUp(true, true, false);
            }
            else if (World.Environment.CanMonsterSpawn())
            {
                WakeUp(false, true, true);
            }
        }
        else if (_sleepTimer > 0)
        {
            ++_sleepTimer;
            if (_sleepTimer >= 110)
            {
                _sleepTimer = 0;
            }
        }
    }

    /// <summary>
    ///     Primary Tick entry.
    /// </summary>
    /// <remarks>
    ///     Events that should occur on both client and server should go in <see cref="GenericTick" />
    /// </remarks>
    public override void Tick()
    {
        TickSleep();
        GenericTick();

        if (World.IsRemote || CurrentScreenHandler == null || CurrentScreenHandler.canUse(this))
        {
            return;
        }

        closeHandledScreen();
        CurrentScreenHandler = PlayerScreenHandler;
    }

    /// <summary>
    ///     Tick events that needs both server and client goes here.
    /// </summary>
    /// <remarks>
    ///     Called from both <see cref="Tick" /> and <see cref="ServerPlayerEntity.PlayerTick" />
    /// </remarks>
    protected void GenericTick()
    {
        base.Tick();
        AfterLivingTickCosmetics();
    }

    /// <summary>
    ///     Cape, play time stat, and minecart state — runs after <see cref="EntityLiving.Tick" /> (or after
    ///     <see cref="EntityLiving.BaseTick" /> on idle server ticks).
    /// </summary>
    protected void AfterLivingTickCosmetics()
    {
        PrevCapePos = CapePos;

        double deltaX = X - CapePos.x;
        double deltaY = Y - CapePos.y;
        double deltaZ = Z - CapePos.z;
        const double teleportThreshold = 10.0D;
        if (Math.Abs(deltaX) > teleportThreshold ||
            Math.Abs(deltaY) > teleportThreshold ||
            Math.Abs(deltaZ) > teleportThreshold)
        {
            PrevCapePos = CapePos = new Vec3D(X, Y, Z);
        }
        else
        {
            CapePos += new Vec3D(deltaX * 0.25D, deltaY * 0.25D, deltaZ * 0.25D);
        }

        IncreaseStat(Stats.Stats.MinutesPlayedStat, 1);

        if (Vehicle == null)
        {
            _startMinecartRidingCoordinate = null;
        }
    }

    protected void PickupAndInventorySubtick()
    {
        if (World.Difficulty == 0 && Health < 20 && Age % 20 * 12 == 0)
        {
            Heal(1);
        }

        Inventory.Tick();
    }

    protected void CollideWithPickupEntities()
    {
        if (Health <= 0) return;

        List<Entity> entities = World.Entities.GetEntities(this, BoundingBox.Expand(1.0D, 0.0D, 1.0D));

        foreach (Entity entity in entities)
        {
            if (!entity.Dead)
            {
                collideWithEntity(entity);
            }
        }
    }

    protected override bool isMovementBlocked() => Health <= 0 || IsSleeping;

    public virtual void closeHandledScreen() => CurrentScreenHandler = PlayerScreenHandler;

    public override void UpdateCloak()
    {
        PlayerCloakUrl = "http://s3.amazonaws.com/MinecraftCloaks/" + Name + ".png";
        CloakUrl = PlayerCloakUrl;
    }

    protected virtual bool isPvpEnabled() => false;

    public override void TickRiding()
    {
        double x = X;
        double y = Y;
        double z = Z;
        base.TickRiding();
        PrevStepBobbingAmount = StepBobbingAmount;
        StepBobbingAmount = 0.0F;
        IncreaseRidingMotionStats(X - x, Y - y, Z - z);
    }

    public override void TeleportToTop()
    {
        StandingEyeHeight = 1.62F;
        SetBoundingBoxSpacing(0.6F, 1.8F);
        base.TeleportToTop();
        Health = 20;
        DeathTime = 0;
    }

    protected override void TickLiving()
    {
        if (HandSwinging)
        {
            ++HandSwingTicks;
            if (HandSwingTicks >= 8)
            {
                HandSwingTicks = 0;
                HandSwinging = false;
            }
        }
        else
        {
            HandSwingTicks = 0;
        }

        SwingAnimationProgress = HandSwingTicks / 8.0F;
    }

    protected override void TickMovement()
    {
        PickupAndInventorySubtick();
        PrevStepBobbingAmount = StepBobbingAmount;
        base.TickMovement();
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        float tiltTarget = (float)Math.Atan(-VelocityY * 0.2F) * 15.0F;
        if (horizontalSpeed > 0.1F)
        {
            horizontalSpeed = 0.1F;
        }

        if (!OnGround || Health <= 0)
        {
            horizontalSpeed = 0.0F;
        }

        if (OnGround || Health <= 0)
        {
            tiltTarget = 0.0F;
        }

        StepBobbingAmount += (horizontalSpeed - StepBobbingAmount) * 0.4F;
        Tilt += (tiltTarget - Tilt) * 0.8F;
        CollideWithPickupEntities();
    }

    private void collideWithEntity(Entity entity) => entity.OnPlayerInteraction(this);

    public int getScore() => Score;

    protected override void OnKilledBy(Entity? adversary)
    {
        base.OnKilledBy(adversary);
        SetBoundingBoxSpacing(0.2F, 0.2F);
        SetPosition(X, Y, Z);
        VelocityY = 0.1F;
        if (Name is "Notch")
        {
            DropItem(new ItemStack(Item.Apple, 1), true);
        }

        Inventory.DropInventory();
        if (adversary != null)
        {
            VelocityX = -MathHelper.Cos((AttackedAtYaw + Yaw) * (float)Math.PI / 180.0F) * 0.1F;
            VelocityZ = -MathHelper.Sin((AttackedAtYaw + Yaw) * (float)Math.PI / 180.0F) * 0.1F;
        }
        else
        {
            VelocityX = VelocityZ = 0.0D;
        }

        StandingEyeHeight = 0.1F;
        IncreaseStat(Stats.Stats.DeathsStat, 1);
    }

    public override void UpdateKilledAchievement(Entity entityKilled, int score)
    {
        Score += score;
        IncreaseStat(entityKilled is EntityPlayer ? Stats.Stats.PlayerKillsStat : Stats.Stats.MobKillsStat, 1);
    }

    public virtual void DropSelectedItem()
    {
        if (GameMode.CanDrop)
        {
            DropItem(Inventory.RemoveStack(Inventory.SelectedSlot, 1));
        }
    }

    /// <summary>
    ///     Drop <see cref="ItemStack" /> into the world
    /// </summary>
    /// <returns>True when item was removed</returns>
    public bool DropItem(ItemStack? stack, bool throwRandomly = false)
    {
        if (!GameMode.CanDrop) return false;
        if (stack == null) return true;

        EntityItem itemEntity = new(World, X, Y - 0.3F + EyeHeight, Z, stack)
        {
            DelayBeforeCanPickup = 40
        };
        if (throwRandomly)
        {
            float randomSpeed = Random.NextFloat() * 0.5F;
            float randomAngle = Random.NextFloat() * (float)Math.PI * 2.0F;
            itemEntity.VelocityX = -MathHelper.Sin(randomAngle) * randomSpeed;
            itemEntity.VelocityZ = MathHelper.Cos(randomAngle) * randomSpeed;
            itemEntity.VelocityY = 0.2F;
        }
        else
        {
            float baseSpeed = 0.3F;
            float randomSpeed = Random.NextFloat() * (float)Math.PI * 2.0F;

            itemEntity.VelocityX = -MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * baseSpeed;
            itemEntity.VelocityZ = MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * baseSpeed;
            itemEntity.VelocityY = -MathHelper.Sin(Pitch / 180.0F * (float)Math.PI) * baseSpeed + 0.1F;

            baseSpeed = 0.02F;
            baseSpeed *= Random.NextFloat();

            itemEntity.VelocityX += Math.Cos(randomSpeed) * baseSpeed;
            itemEntity.VelocityY += (Random.NextFloat() - Random.NextFloat()) * 0.1F;
            itemEntity.VelocityZ += Math.Sin(randomSpeed) * baseSpeed;
        }

        SpawnItem(itemEntity);
        IncreaseStat(Stats.Stats.DropStat, 1);

        return true;
    }

    protected virtual void SpawnItem(EntityItem itemEntity) => World.SpawnEntity(itemEntity);

    public float GetBlockBreakingSpeed(Block block)
    {
        float breakingSpeed = Inventory.GetStrVsBlock(block);
        if (IsInFluid(Material.Water))
        {
            breakingSpeed /= 5.0F;
        }

        if (!OnGround)
        {
            breakingSpeed /= 5.0F;
        }

        return breakingSpeed;
    }

    public bool CanHarvest(Block block) => Inventory.CanHarvestBlock(block);

    protected override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        NBTTagList inventoryTagList = nbt.GetTagList("Inventory");
        Inventory.ReadFromNBT(inventoryTagList);
        DimensionId = nbt.GetInteger("Dimension");
        Sleeping = nbt.GetBoolean("Sleeping");
        _sleepTimer = nbt.GetShort("SleepTimer");
        if (Sleeping)
        {
            SleepingPos = new Vec3i(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z));
            WakeUp(true, true, false);
        }

        if (nbt.HasKey("SpawnX") && nbt.HasKey("SpawnY") && nbt.HasKey("SpawnZ"))
        {
            _playerSpawnCoordinate = new Vec3i(nbt.GetInteger("SpawnX"), nbt.GetInteger("SpawnY"), nbt.GetInteger("SpawnZ"));
        }
    }

    protected override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetTag("Inventory", Inventory.WriteToNBT(new NBTTagList()));
        nbt.SetInteger("Dimension", DimensionId);
        nbt.SetBoolean("Sleeping", Sleeping);
        nbt.SetShort("SleepTimer", (short)_sleepTimer);
        if (_playerSpawnCoordinate is not var (x, y, z)) return;
        nbt.SetInteger("SpawnX", x);
        nbt.SetInteger("SpawnY", y);
        nbt.SetInteger("SpawnZ", z);
    }

    public virtual void openChestScreen(IInventory inventory)
    {
    }

    public virtual void openCraftingScreen(int x, int y, int z)
    {
    }

    public virtual void sendPickup(Entity item, int count)
    {
    }

    protected virtual void resetEyeHeight() => StandingEyeHeight = 1.62F;

    public override bool Damage(Entity? damageSource, int amount)
    {
        if (!GameMode.CanReceiveDamage) return false;

        EntityAge = 0;
        if (Health <= 0) return false;

        if (IsSleeping && !World.IsRemote)
        {
            WakeUp(true, true, false);
        }

        if (damageSource is EntityMonster or EntityArrow)
        {
            amount = World.Difficulty switch
            {
                0 => 0,
                1 => amount / 3 + 1,
                3 => amount * 3 / 2,
                _ => amount
            };
        }

        if (amount == 0) return false;

        if (damageSource is EntityArrow { Owner: not null } arrow)
        {
            damageSource = arrow.Owner;
        }

        if (damageSource is EntityLiving living)
        {
            CommandWolvesToAttack(living, false);
        }

        IncreaseStat(Stats.Stats.DamageTakenStat, amount);
        return base.Damage(damageSource, amount);
    }

    private void CommandWolvesToAttack(EntityLiving entity, bool sitting)
    {
        switch (entity)
        {
            case EntityCreeper or EntityGhast:
            case EntityWolf { IsWolfTamed: true } wolf when Name != null && Name.Equals(wolf.WolfOwner):
            case EntityPlayer p when (!isPvpEnabled() || !p.GameMode.CanBeTargeted):
                return;
        }

        List<EntityWolf> wolves = World.Entities.CollectEntitiesOfType<EntityWolf>(new Box(X, Y, Z, X + 1.0D, Y + 1.0D, Z + 1.0D).Expand(16.0D, 4.0D, 16.0D));

        foreach (EntityWolf wolf in wolves)
        {
            if (!wolf.IsWolfTamed) continue;
            if (wolf.Target != null) continue;
            if (Name != null && !Name.Equals(wolf.WolfOwner)) continue;
            if (sitting && wolf.IsWolfSitting) continue;
            wolf.IsWolfSitting = false;
            wolf.Target = entity;
        }
    }

    protected override void ApplyDamage(int amount)
    {
        int armorReduction = 25 - Inventory.GetTotalArmorValue();
        int scaledDamage = amount * armorReduction + _damageSpill;
        Inventory.DamageArmor(amount);
        amount = scaledDamage / 25;
        _damageSpill = scaledDamage % 25;
        base.ApplyDamage(amount);
    }

    public virtual void openFurnaceScreen(BlockEntityFurnace furnace)
    {
    }

    public virtual void openDispenserScreen(BlockEntityDispenser dispenser)
    {
    }

    public virtual void openEditSignScreen(BlockEntitySign sign)
    {
    }

    public void Interact(Entity entity)
    {
        if (!GameMode.CanInteract) return;

        if (entity.Interact(this)) return;

        ItemStack? itemStackInHand = GetHand();
        if (itemStackInHand == null || entity is not EntityLiving living) return;

        itemStackInHand.useOnEntity(living, this);
        if (itemStackInHand.Count > 0) return;

        ItemStack.onRemoved(this);
        ClearStackInHand();
    }

    public ItemStack? GetHand() => Inventory.ItemInHand;

    public void ClearStackInHand() => Inventory.SetStack(Inventory.SelectedSlot, null);

    public virtual void SwingHand()
    {
        HandSwingTicks = -1;
        HandSwinging = true;
    }

    public void Attack(Entity target)
    {
        if (!GameMode.CanInflictDamage) return;

        int damage = Inventory.GetDamageVsEntity(target);
        if (damage <= 0) return;

        if (VelocityY < 0.0D)
        {
            ++damage;
        }

        target.Damage(this, damage);
        if (target is not EntityLiving living) return;

        ItemStack? itemStackInHand = GetHand();
        if (itemStackInHand != null)
        {
            itemStackInHand.postHit(living, this);
            if (itemStackInHand.Count <= 0)
            {
                ItemStack.onRemoved(this);
                ClearStackInHand();
            }
        }

        if (living.IsAlive)
        {
            CommandWolvesToAttack(living, true);
        }

        IncreaseStat(Stats.Stats.DamageDealtStat, damage);
    }

    public virtual void Respawn()
    {
    }

    public abstract void Spawn();

    public virtual void OnCursorStackChanged(ItemStack? stack)
    {
    }

    public override void MarkDead()
    {
        base.MarkDead();
        PlayerScreenHandler.onClosed(this);
        CurrentScreenHandler?.onClosed(this);
    }

    public override bool IsInsideWall() => !Sleeping && base.IsInsideWall();

    public virtual SleepAttemptResult TrySleep(int x, int y, int z)
    {
        if (!World.IsRemote)
        {
            if (IsSleeping || !IsAlive)
            {
                return SleepAttemptResult.OTHER_PROBLEM;
            }

            if (World.Dimension.IsNether)
            {
                return SleepAttemptResult.NOT_POSSIBLE_HERE;
            }

            if (World.Environment.CanMonsterSpawn())
            {
                return SleepAttemptResult.NOT_POSSIBLE_NOW;
            }

            if (Math.Abs(X - x) > 3.0D || Math.Abs(Y - y) > 2.0D || Math.Abs(Z - z) > 3.0D)
            {
                return SleepAttemptResult.TOO_FAR_AWAY;
            }
        }

        SetBoundingBoxSpacing(0.2F, 0.2F);
        StandingEyeHeight = 0.2F;
        if (World.Reader.IsPosLoaded(x, y, z))
        {
            int bedMeta = World.Reader.GetBlockMeta(x, y, z);
            int bedDirection = BlockBed.getDirection(bedMeta);
            float sleepX = 0.5F;
            float sleepZ = 0.5F;
            switch (bedDirection)
            {
                case 0:
                    sleepZ = 0.9F;
                    break;
                case 1:
                    sleepX = 0.1F;
                    break;
                case 2:
                    sleepZ = 0.1F;
                    break;
                case 3:
                    sleepX = 0.9F;
                    break;
            }

            calculateSleepOffset(bedDirection);
            SetPosition(x + sleepX, y + 15.0F / 16.0F, z + sleepZ);
        }
        else
        {
            SetPosition(x + 0.5F, y + 15.0F / 16.0F, z + 0.5F);
        }

        Sleeping = true;
        _sleepTimer = 0;
        SleepingPos = new Vec3i(x, y, z);
        VelocityX = VelocityZ = VelocityY = 0.0D;
        if (!World.IsRemote)
        {
            World.Entities.UpdateSleepingPlayers();
        }

        return SleepAttemptResult.OK;
    }

    private void calculateSleepOffset(int bedDir)
    {
        SleepOffsetX = 0.0F;
        SleepOffsetZ = 0.0F;
        switch (bedDir)
        {
            case 0:
                SleepOffsetZ = -1.8F;
                break;
            case 1:
                SleepOffsetX = 1.8F;
                break;
            case 2:
                SleepOffsetZ = 1.8F;
                break;
            case 3:
                SleepOffsetX = -1.8F;
                break;
        }
    }

    public virtual void WakeUp(bool resetSleepTimer, bool updateSleepingPlayers, bool setSpawnPos)
    {
        SetBoundingBoxSpacing(0.6F, 1.8F);
        resetEyeHeight();
        Vec3i? bedPos = SleepingPos;
        if (bedPos is var (x, y, z) && World.Reader.GetBlockId(x, y, z) == Block.Bed.id)
        {
            int bedMeta = World.Reader.GetBlockMeta(x, y, z);
            BlockBed.updateState(World.Writer, x, y, z, bedMeta, false);
            Vec3i? wakeUpPos = BlockBed.findWakeUpPosition(World.Reader, x, y, z, 0) ?? new Vec3i(x, y + 1, z);
            SetPosition(wakeUpPos.Value.X + 0.5F, wakeUpPos.Value.Y + StandingEyeHeight + 0.1F, wakeUpPos.Value.Z + 0.5F);
        }

        Sleeping = false;
        if (!World.IsRemote && updateSleepingPlayers)
        {
            World.Entities.UpdateSleepingPlayers();
        }

        _sleepTimer = resetSleepTimer ? 0 : 100;

        if (setSpawnPos)
        {
            SetSpawnPos(SleepingPos);
        }
    }

    private bool IsSleepingInBed() => SleepingPos != null && World.Reader.GetBlockId(SleepingPos.Value.X, SleepingPos.Value.Y, SleepingPos.Value.Z) == Block.Bed.id;

    public static Vec3i? FindRespawnPosition(IWorldContext world, Vec3i? spawnPos)
    {
        if (spawnPos is not var (x, y, z)) return null;

        IChunkSource chunkSource = world.ChunkHost.ChunkSource;

        chunkSource.LoadChunk((x - 3) >> 4, (z - 3) >> 4);
        chunkSource.LoadChunk((x + 3) >> 4, (z - 3) >> 4);
        chunkSource.LoadChunk((x - 3) >> 4, (z + 3) >> 4);
        chunkSource.LoadChunk((x + 3) >> 4, (z + 3) >> 4);

        return world.Reader.GetBlockId(x, y, z) != Block.Bed.id ? null : BlockBed.findWakeUpPosition(world.Reader, x, y, z, 0);
    }

    public float GetSleepingRotation()
    {
        if (SleepingPos == null) return 0.0F;

        int blockMeta = World.Reader.GetBlockMeta(SleepingPos.Value.X, SleepingPos.Value.Y, SleepingPos.Value.Z);
        int direction = BlockBed.getDirection(blockMeta);
        return direction switch
        {
            0 => 90.0F,
            1 => 0.0F,
            2 => 270.0F,
            3 => 180.0F,
            _ => 0.0F
        };
    }

    public bool IsPlayerFullyAsleep() => Sleeping && _sleepTimer >= 100;

    public virtual void SendMessage(string msg)
    {
    }

    public Vec3i? GetSpawnPos() => _playerSpawnCoordinate;

    public void SetSpawnPos(Vec3i? spawnPos)
    {
        if (spawnPos is var (x, y, z)) _playerSpawnCoordinate = new Vec3i(x, y, z);
        else _playerSpawnCoordinate = null;
    }

    public void IncrementStat(StatBase stat) => IncreaseStat(stat, 1);

    public virtual void IncreaseStat(StatBase stat, int amount)
    {
    }

    protected override void Jump()
    {
        base.Jump();
        IncreaseStat(Stats.Stats.JumpStat, 1);
    }

    protected override void Travel(float x, float z)
    {
        double startX = X;
        double startY = Y;
        double startZ = Z;
        base.Travel(x, z);
        UpdateMovementStat(X - startX, Y - startY, Z - startZ);
    }

    private void UpdateMovementStat(double x, double y, double z)
    {
        if (Vehicle != null) return;

        int distanceScaled;
        if (IsInFluid(Material.Water))
        {
            distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + y * y + z * z) * 100.0F);
            if (distanceScaled > 0)
            {
                IncreaseStat(Stats.Stats.DistanceDoveStat, distanceScaled);
            }
        }
        else if (IsInWater)
        {
            distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + z * z) * 100.0F);
            if (distanceScaled > 0)
            {
                IncreaseStat(Stats.Stats.DistanceSwumStat, distanceScaled);
            }
        }
        else if (IsOnLadder)
        {
            if (y > 0.0D)
            {
                IncreaseStat(Stats.Stats.DistanceClimbedStat, (int)MathHelper.Round(y * 100.0D));
            }
        }
        else if (OnGround)
        {
            distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + z * z) * 100.0F);
            if (distanceScaled > 0)
            {
                IncreaseStat(Stats.Stats.DistanceWalkedStat, distanceScaled);
            }
        }
        else
        {
            distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + z * z) * 100.0F);
            if (distanceScaled > 25)
            {
                IncreaseStat(Stats.Stats.DistanceFlownStat, distanceScaled);
            }
        }
    }

    private void IncreaseRidingMotionStats(double x, double y, double z)
    {
        if (Vehicle is null) return;

        int distanceScaled = (int)Math.Round(Math.Sqrt(x * x + y * y + z * z) * 100.0);

        if (distanceScaled <= 0) return;

        switch (Vehicle)
        {
            case EntityMinecart:
                IncreaseStat(Stats.Stats.DistanceByMinecartStat, distanceScaled);

                int currentX = MathHelper.Floor(X);
                int currentY = MathHelper.Floor(Y);
                int currentZ = MathHelper.Floor(Z);

                if (_startMinecartRidingCoordinate is null)
                {
                    _startMinecartRidingCoordinate = new Vec3i(currentX, currentY, currentZ);
                }
                else if (_startMinecartRidingCoordinate.Value.SquaredDistanceTo(new Vec3i(currentX, currentY, currentZ)) >= 1_000_000)
                {
                    IncreaseStat(Achievements.CraftRail, 1);
                }

                break;

            case EntityBoat:
                IncreaseStat(Stats.Stats.DistanceByBoatStat, distanceScaled);
                break;

            case EntityPig:
                IncreaseStat(Stats.Stats.DistanceByPigStat, distanceScaled);
                break;
        }
    }

    protected override void OnLanding(float fallDistance)
    {
        if (fallDistance >= 2.0F)
        {
            IncreaseStat(Stats.Stats.DistanceFallenStat, (int)MathHelper.Round(fallDistance * 100.0D));
        }

        base.OnLanding(fallDistance);
    }

    public override void OnKillOther(EntityLiving entityLiving)
    {
        if (entityLiving is EntityMonster)
        {
            IncrementStat(Achievements.KillEnemy);
        }
    }

    public override int GetItemStackTextureId(ItemStack stack)
    {
        int textureId = base.GetItemStackTextureId(stack);
        if (stack.ItemId == Item.FishingRod.id && FishHook != null)
        {
            textureId = stack.getTextureId() + 16;
        }

        return textureId;
    }

    public override void TickPortalCooldown()
    {
        if (PortalCooldown <= 0)
        {
            InTeleportationState = true;
        }
    }

    public virtual void SendChatMessage(string message)
    {
    }
}
