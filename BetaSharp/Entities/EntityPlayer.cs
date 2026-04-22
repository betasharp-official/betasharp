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
    public InventoryPlayer inventory;
    public ScreenHandler playerScreenHandler;
    public ScreenHandler currentScreenHandler;
    public byte unused = 0;
    public int score;
    public float prevStepBobbingAmount;
    public float stepBobbingAmount;
    public bool handSwinging;
    public int handSwingTicks;
    public string name;
    public int dimensionId;
    public string playerCloakUrl;
    public double prevCapeX;
    public double prevCapeY;
    public double prevCapeZ;
    public double capeX;
    public double capeY;
    public double capeZ;
    protected bool sleeping;
    public Vec3i sleepingPos;
    private int sleepTimer;
    public float sleepOffsetX;
    public float sleepOffsetY;
    public float sleepOffsetZ;
    private Vec3i? playerSpawnCoordinate;
    private Vec3i? startMinecartRidingCoordinate;
    public int portalCooldown = 20;
    protected bool inTeleportationState;
    public float changeDimensionCooldown;
    public float lastScreenDistortion;
    private int damageSpill;
    public EntityFish fishHook = null;

    /// <summary>
    /// The player's current game mode value. All reads go through the holder, so if the
    /// server resyncs registry data the value updates in-place without requiring a
    /// separate update packet.
    /// </summary>
    public GameMode GameMode => GameModeHolder.Value;

    /// <summary>
    /// The holder backing <see cref="GameMode"/>.
    /// </summary>
    public Holder<GameMode> GameModeHolder { get; set; } = new(new GameMode());

    public EntityPlayer(IWorldContext world) : base(world)
    {
        inventory = new InventoryPlayer(this);
        playerScreenHandler = new PlayerScreenHandler(inventory, !world.IsRemote);
        currentScreenHandler = playerScreenHandler;
        StandingEyeHeight = 1.62F;
        Vec3i spawnPos = world.Properties.GetSpawnPos();
        SetPositionAndAnglesKeepPrevAngles((double)spawnPos.X + 0.5D, (double)(spawnPos.Y + 1), (double)spawnPos.Z + 0.5D, 0.0F, 0.0F);
        Health = 20;
        ModelName = "humanoid";
        RotationOffset = 180.0F;
        FireImmunityTicks = 20;
        Texture = "/mob/char.png";
    }

    public override bool canBreatheUnderwater() => !GameMode.NeedsAir;

    protected void TickSleep()
    {
        if (isSleeping())
        {
            ++sleepTimer;
            if (sleepTimer > 100)
            {
                sleepTimer = 100;
            }

            if (!World.IsRemote)
            {
                if (!isSleepingInBed())
                {
                    wakeUp(true, true, false);
                }
                else if (World.Environment.CanMonsterSpawn())
                {
                    wakeUp(false, true, true);
                }
            }
        }
        else if (sleepTimer > 0)
        {
            ++sleepTimer;
            if (sleepTimer >= 110)
            {
                sleepTimer = 0;
            }
        }
    }

    /// <summary>
    /// Primary Tick entry.
    /// </summary>
    /// <remarks>
    /// Events that should occur on both client and server should go in <see cref="GenericTick"/>
    /// </remarks>
    public override void Tick()
    {
        TickSleep();
        GenericTick();

        if (!World.IsRemote && currentScreenHandler != null && !currentScreenHandler.canUse(this))
        {
            closeHandledScreen();
            currentScreenHandler = playerScreenHandler;
        }
    }

    /// <summary>
    /// Tick events that needs both server and client goes here.
    /// </summary>
    /// <remarks>
    /// Called from both <see cref="Tick"/> and <see cref="ServerPlayerEntity.PlayerTick"/>
    /// </remarks>
    protected void GenericTick()
    {
        base.Tick();
        AfterLivingTickCosmetics();
    }

    /// <summary>
    /// Cape, play time stat, and minecart state — runs after <see cref="EntityLiving.Tick"/> (or after <see cref="EntityLiving.BaseTick"/> on idle server ticks).
    /// </summary>
    protected void AfterLivingTickCosmetics()
    {
        prevCapeX = capeX;
        prevCapeY = capeY;
        prevCapeZ = capeZ;
        double capeDeltaX = X - capeX;
        double capeDeltaY = Y - capeY;
        double capeDeltaZ = Z - capeZ;
        double maxCapeDelta = 10.0D;
        if (capeDeltaX > maxCapeDelta)
        {
            prevCapeX = capeX = X;
        }

        if (capeDeltaZ > maxCapeDelta)
        {
            prevCapeZ = capeZ = Z;
        }

        if (capeDeltaY > maxCapeDelta)
        {
            prevCapeY = capeY = Y;
        }

        if (capeDeltaX < -maxCapeDelta)
        {
            prevCapeX = capeX = X;
        }

        if (capeDeltaZ < -maxCapeDelta)
        {
            prevCapeZ = capeZ = Z;
        }

        if (capeDeltaY < -maxCapeDelta)
        {
            prevCapeY = capeY = Y;
        }

        capeX += capeDeltaX * 0.25D;
        capeZ += capeDeltaZ * 0.25D;
        capeY += capeDeltaY * 0.25D;
        increaseStat(Stats.Stats.MinutesPlayedStat, 1);
        if (Vehicle == null)
        {
            startMinecartRidingCoordinate = null;
        }
    }

    protected void PickupAndInventorySubtick()
    {
        if (World.Difficulty == 0 && Health < 20 && Age % 20 * 12 == 0)
        {
            heal(1);
        }

        inventory.Tick();
    }

    protected void CollideWithPickupEntities()
    {
        if (Health <= 0) return;

        var entities = World.Entities.GetEntities(this, BoundingBox.Expand(1.0D, 0.0D, 1.0D));

        foreach (var entity in entities)
        {
            if (!entity.Dead)
            {
                collideWithEntity(entity);
            }
        }
    }

    protected override bool isMovementBlocked()
    {
        return Health <= 0 || isSleeping();
    }

    public virtual void closeHandledScreen()
    {
        currentScreenHandler = playerScreenHandler;
    }

    public override void UpdateCloak()
    {
        playerCloakUrl = string.IsNullOrWhiteSpace(name) ? string.Empty : $"{name}#cape";
        CloakUrl = playerCloakUrl;
    }

    protected virtual bool isPvpEnabled()
    {
        return false;
    }

    public override void TickRiding()
    {
        double startX = X;
        double startY = Y;
        double startZ = Z;
        base.TickRiding();
        prevStepBobbingAmount = stepBobbingAmount;
        stepBobbingAmount = 0.0F;
        increaseRidingMotionStats(X - startX, Y - startY, Z - startZ);
    }

    public override void TeleportToTop()
    {
        StandingEyeHeight = 1.62F;
        SetBoundingBoxSpacing(0.6F, 1.8F);
        base.TeleportToTop();
        Health = 20;
        DeathTime = 0;
    }

    public override void tickLiving()
    {
        if (handSwinging)
        {
            ++handSwingTicks;
            if (handSwingTicks >= 8)
            {
                handSwingTicks = 0;
                handSwinging = false;
            }
        }
        else
        {
            handSwingTicks = 0;
        }

        SwingAnimationProgress = (float)handSwingTicks / 8.0F;
    }

    public override void tickMovement()
    {
        PickupAndInventorySubtick();
        prevStepBobbingAmount = stepBobbingAmount;
        base.tickMovement();
        float horizontalSpeed = MathHelper.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
        float tiltTarget = (float)Math.Atan(-VelocityY * (double)0.2F) * 15.0F;
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

        stepBobbingAmount += (horizontalSpeed - stepBobbingAmount) * 0.4F;
        Tilt += (tiltTarget - Tilt) * 0.8F;
        CollideWithPickupEntities();
    }

    private void collideWithEntity(Entity entity)
    {
        entity.OnPlayerInteraction(this);
    }

    public int getScore()
    {
        return score;
    }

    public override void onKilledBy(Entity entity)
    {
        base.onKilledBy(entity);
        SetBoundingBoxSpacing(0.2F, 0.2F);
        SetPosition(X, Y, Z);
        VelocityY = (double)0.1F;
        if (name.Equals("Notch"))
        {
            DropItem(new ItemStack(Item.Apple, 1), true);
        }

        inventory.DropInventory();
        if (entity != null)
        {
            VelocityX = (double)(-MathHelper.Cos((AttackedAtYaw + Yaw) * (float)Math.PI / 180.0F) * 0.1F);
            VelocityZ = (double)(-MathHelper.Sin((AttackedAtYaw + Yaw) * (float)Math.PI / 180.0F) * 0.1F);
        }
        else
        {
            VelocityX = VelocityZ = 0.0D;
        }

        StandingEyeHeight = 0.1F;
        increaseStat(Stats.Stats.DeathsStat, 1);
    }

    public override void UpdateKilledAchievement(Entity entityKilled, int score)
    {
        this.score += score;
        if (entityKilled is EntityPlayer)
        {
            increaseStat(Stats.Stats.PlayerKillsStat, 1);
        }
        else
        {
            increaseStat(Stats.Stats.MobKillsStat, 1);
        }
    }

    public virtual void DropSelectedItem()
    {
        if (GameMode.CanDrop)
        {
            DropItem(inventory.RemoveStack(inventory.SelectedSlot, 1), false);
        }
    }

    /// <summary>
    /// Drop <see cref="ItemStack"/> into the world
    /// </summary>
    /// <returns>True when item was removed</returns>
    public bool DropItem(ItemStack? stack, bool throwRandomly = false)
    {
        if (!GameMode.CanDrop) return false;
        if (stack == null) return true;

        EntityItem itemEntity = new EntityItem(World, X, Y - (double)0.3F + (double)GetEyeHeight(), Z, stack);
        itemEntity.delayBeforeCanPickup = 40;
        float baseSpeed = 0.1F;
        float randomSpeed;
        if (throwRandomly)
        {
            randomSpeed = Random.NextFloat() * 0.5F;
            float randomAngle = Random.NextFloat() * (float)Math.PI * 2.0F;
            itemEntity.VelocityX = (double)(-MathHelper.Sin(randomAngle) * randomSpeed);
            itemEntity.VelocityZ = (double)(MathHelper.Cos(randomAngle) * randomSpeed);
            itemEntity.VelocityY = (double)0.2F;
        }
        else
        {
            baseSpeed = 0.3F;
            itemEntity.VelocityX = (double)(-MathHelper.Sin(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * baseSpeed);
            itemEntity.VelocityZ = (double)(MathHelper.Cos(Yaw / 180.0F * (float)Math.PI) * MathHelper.Cos(Pitch / 180.0F * (float)Math.PI) * baseSpeed);
            itemEntity.VelocityY = (double)(-MathHelper.Sin(Pitch / 180.0F * (float)Math.PI) * baseSpeed + 0.1F);
            baseSpeed = 0.02F;
            randomSpeed = Random.NextFloat() * (float)Math.PI * 2.0F;
            baseSpeed *= Random.NextFloat();
            itemEntity.VelocityX += Math.Cos((double)randomSpeed) * (double)baseSpeed;
            itemEntity.VelocityY += (double)((Random.NextFloat() - Random.NextFloat()) * 0.1F);
            itemEntity.VelocityZ += Math.Sin((double)randomSpeed) * (double)baseSpeed;
        }

        spawnItem(itemEntity);
        increaseStat(Stats.Stats.DropStat, 1);

        return true;
    }

    protected virtual void spawnItem(EntityItem itemEntity)
    {
        World.SpawnEntity(itemEntity);
    }

    public float getBlockBreakingSpeed(Block block)
    {
        float breakingSpeed = inventory.GetStrVsBlock(block);
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

    public bool canHarvest(Block block)
    {
        return inventory.CanHarvestBlock(block);
    }

    public override void ReadNbt(NBTTagCompound nbt)
    {
        base.ReadNbt(nbt);
        NBTTagList inventoryTagList = nbt.GetTagList("Inventory");
        inventory.ReadFromNBT(inventoryTagList);
        dimensionId = nbt.GetInteger("Dimension");
        sleeping = nbt.GetBoolean("Sleeping");
        sleepTimer = nbt.GetShort("SleepTimer");
        if (sleeping)
        {
            sleepingPos = new Vec3i(MathHelper.Floor(X), MathHelper.Floor(Y), MathHelper.Floor(Z));
            wakeUp(true, true, false);
        }

        if (nbt.HasKey("SpawnX") && nbt.HasKey("SpawnY") && nbt.HasKey("SpawnZ"))
        {
            playerSpawnCoordinate = new Vec3i(nbt.GetInteger("SpawnX"), nbt.GetInteger("SpawnY"), nbt.GetInteger("SpawnZ"));
        }
    }

    public override void WriteNbt(NBTTagCompound nbt)
    {
        base.WriteNbt(nbt);
        nbt.SetTag("Inventory", inventory.WriteToNBT(new NBTTagList()));
        nbt.SetInteger("Dimension", dimensionId);
        nbt.SetBoolean("Sleeping", sleeping);
        nbt.SetShort("SleepTimer", (short)sleepTimer);
        if (playerSpawnCoordinate is (int x, int y, int z))
        {
            nbt.SetInteger("SpawnX", x);
            nbt.SetInteger("SpawnY", y);
            nbt.SetInteger("SpawnZ", z);
        }
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

    public override float GetEyeHeight()
    {
        return 0.12F;
    }

    protected virtual void resetEyeHeight()
    {
        StandingEyeHeight = 1.62F;
    }

    public override bool Damage(Entity? damageSource, int amount)
    {
        if (!GameMode.CanReceiveDamage) return false;

        EntityAge = 0;
        if (Health <= 0)
        {
            return false;
        }
        else
        {
            if (isSleeping() && !World.IsRemote)
            {
                wakeUp(true, true, false);
            }

            if (damageSource is EntityMonster || damageSource is EntityArrow)
            {
                switch (World.Difficulty)
                {
                    case 0:
                        amount = 0;
                        break;
                    case 1:
                        amount = amount / 3 + 1;
                        break;
                    case 3:
                        amount = amount * 3 / 2;
                        break;
                }
            }

            if (amount == 0)
            {
                return false;
            }

            if (damageSource is EntityArrow && ((EntityArrow)damageSource).owner != null)
            {
                damageSource = ((EntityArrow)damageSource).owner;
            }

            if (damageSource is EntityLiving)
            {
                commandWolvesToAttack((EntityLiving)damageSource, false);
            }

            increaseStat(Stats.Stats.DamageTakenStat, amount);
            return base.Damage(damageSource, amount);
        }
    }

    protected void commandWolvesToAttack(EntityLiving entity, bool sitting)
    {
        if (entity is not EntityCreeper && entity is not EntityGhast)
        {
            if (entity is EntityWolf wolf)
            {
                if (wolf.isWolfTamed() && name.Equals(wolf.getWolfOwner()))
                {
                    return;
                }
            }

            if (entity is not EntityPlayer p || isPvpEnabled() && p.GameMode.CanBeTargeted)
            {
                var ownedWolves = World.Entities.CollectEntitiesOfType<EntityWolf>(new Box(X, Y, Z, X + 1.0D, Y + 1.0D, Z + 1.0D).Expand(16.0D, 4.0D, 16.0D));

                foreach (EntityWolf ownedWolf in ownedWolves)
                {
                    if (!ownedWolf.isWolfTamed()) continue;
                    if (ownedWolf.getTarget() != null) continue;
                    if (!name.Equals(ownedWolf.getWolfOwner())) continue;
                    if (sitting && ownedWolf.isWolfSitting()) continue;

                    ownedWolf.setWolfSitting(false);
                    ownedWolf.setTarget(entity);
                }
            }
        }
    }

    protected override void applyDamage(int amount)
    {
        int armorReduction = 25 - inventory.GetTotalArmorValue();
        int scaledDamage = amount * armorReduction + damageSpill;
        inventory.DamageArmor(amount);
        amount = scaledDamage / 25;
        damageSpill = scaledDamage % 25;
        base.applyDamage(amount);
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

    public void interact(Entity entity)
    {
        if (!GameMode.CanInteract) return;
        if (!entity.Interact(this))
        {
            ItemStack itemStackInHand = getHand();
            if (itemStackInHand != null && entity is EntityLiving living)
            {
                itemStackInHand.useOnEntity(living, this);
                if (itemStackInHand.Count <= 0)
                {
                    ItemStack.onRemoved(this);
                    clearStackInHand();
                }
            }
        }
    }

    public ItemStack getHand()
    {
        return inventory.GetItemInHand();
    }

    public void clearStackInHand()
    {
        inventory.SetStack(inventory.SelectedSlot, (ItemStack)null);
    }

    public override double GetStandingEyeHeight()
    {
        return (double)(StandingEyeHeight - 0.5F);
    }

    public virtual void swingHand()
    {
        handSwingTicks = -1;
        handSwinging = true;
    }

    public void attack(Entity target)
    {
        if (!GameMode.CanInflictDamage) return;
        int damage = inventory.GetDamageVsEntity(target);
        if (damage > 0)
        {
            if (VelocityY < 0.0D)
            {
                ++damage;
            }

            target.Damage(this, damage);
            if (target is EntityLiving living)
            {
                ItemStack itemStackInHand = getHand();
                if (itemStackInHand != null)
                {
                    itemStackInHand.postHit(living, this);
                    if (itemStackInHand.Count <= 0)
                    {
                        ItemStack.onRemoved(this);
                        clearStackInHand();
                    }
                }

                if (living.IsAlive())
                {
                    commandWolvesToAttack(living, true);
                }

                increaseStat(Stats.Stats.DamageDealtStat, damage);
            }
        }
    }

    public virtual void respawn()
    {
    }

    public abstract void spawn();

    public virtual void onCursorStackChanged(ItemStack? stack)
    {
    }

    public override void MarkDead()
    {
        base.MarkDead();
        playerScreenHandler.onClosed(this);
        if (currentScreenHandler != null)
        {
            currentScreenHandler.onClosed(this);
        }
    }

    public override bool IsInsideWall()
    {
        return !sleeping && base.IsInsideWall();
    }

    public virtual SleepAttemptResult trySleep(int x, int y, int z)
    {
        if (!World.IsRemote)
        {
            if (isSleeping() || !IsAlive())
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

            if (Math.Abs(base.X - (double)x) > 3.0D || Math.Abs(base.Y - (double)y) > 2.0D || Math.Abs(base.Z - (double)z) > 3.0D)
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
            SetPosition((double)((float)x + sleepX), (double)((float)y + 15.0F / 16.0F), (double)((float)z + sleepZ));
        }
        else
        {
            SetPosition((double)((float)x + 0.5F), (double)((float)y + 15.0F / 16.0F), (double)((float)z + 0.5F));
        }

        sleeping = true;
        sleepTimer = 0;
        sleepingPos = new Vec3i(x, y, z);
        VelocityX = VelocityZ = VelocityY = 0.0D;
        if (!World.IsRemote)
        {
            World.Entities.UpdateSleepingPlayers();
        }

        return SleepAttemptResult.OK;
    }

    private void calculateSleepOffset(int bedDir)
    {
        sleepOffsetX = 0.0F;
        sleepOffsetZ = 0.0F;
        switch (bedDir)
        {
            case 0:
                sleepOffsetZ = -1.8F;
                break;
            case 1:
                sleepOffsetX = 1.8F;
                break;
            case 2:
                sleepOffsetZ = 1.8F;
                break;
            case 3:
                sleepOffsetX = -1.8F;
                break;
        }
    }

    public virtual void wakeUp(bool resetSleepTimer, bool updateSleepingPlayers, bool setSpawnPos)
    {
        SetBoundingBoxSpacing(0.6F, 1.8F);
        resetEyeHeight();
        Vec3i? bedPos = sleepingPos;
        if (bedPos is (int x, int y, int z) && World.Reader.GetBlockId(x, y, z) == Block.Bed.id)
        {
            int bedMeta = World.Reader.GetBlockMeta(x, y, z);
            BlockBed.updateState(World.Writer, x, y, z, bedMeta, false);
            Vec3i? wakeUpPos = BlockBed.findWakeUpPosition(World.Reader, x, y, z, 0);
            if (wakeUpPos == null)
            {
                wakeUpPos = new Vec3i(x, y + 1, z);
            }

            SetPosition(wakeUpPos.Value.X + 0.5F, wakeUpPos.Value.Y + StandingEyeHeight + 0.1F, wakeUpPos.Value.Z + 0.5F);
        }

        sleeping = false;
        if (!World.IsRemote && updateSleepingPlayers)
        {
            World.Entities.UpdateSleepingPlayers();
        }

        if (resetSleepTimer)
        {
            sleepTimer = 0;
        }
        else
        {
            sleepTimer = 100;
        }

        if (setSpawnPos)
        {
            this.setSpawnPos(sleepingPos);
        }
    }

    private bool isSleepingInBed()
    {
        return World.Reader.GetBlockId(sleepingPos.X, sleepingPos.Y, sleepingPos.Z) == Block.Bed.id;
    }

    public static Vec3i? findRespawnPosition(IWorldContext world, Vec3i? spawnPos)
    {
        if (spawnPos is not (int x, int y, int z))
        {
            return null;
        }

        IChunkSource chunkSource = world.ChunkHost.ChunkSource;

        chunkSource.LoadChunk((x - 3) >> 4, (z - 3) >> 4);
        chunkSource.LoadChunk((x + 3) >> 4, (z - 3) >> 4);
        chunkSource.LoadChunk((x - 3) >> 4, (z + 3) >> 4);
        chunkSource.LoadChunk((x + 3) >> 4, (z + 3) >> 4);

        if (world.Reader.GetBlockId(x, y, z) != Block.Bed.id)
        {
            return null;
        }

        return BlockBed.findWakeUpPosition(world.Reader, x, y, z, 0);
    }

    public float getSleepingRotation()
    {
        if (sleepingPos != null)
        {
            int bedMeta = World.Reader.GetBlockMeta(sleepingPos.X, sleepingPos.Y, sleepingPos.Z);
            int bedDirection = BlockBed.getDirection(bedMeta);
            switch (bedDirection)
            {
                case 0:
                    return 90.0F;
                case 1:
                    return 0.0F;
                case 2:
                    return 270.0F;
                case 3:
                    return 180.0F;
            }
        }

        return 0.0F;
    }

    public override bool isSleeping()
    {
        return sleeping;
    }

    public bool isPlayerFullyAsleep()
    {
        return sleeping && sleepTimer >= 100;
    }

    public int getSleepTimer()
    {
        return sleepTimer;
    }

    public virtual void sendMessage(string msg)
    {
    }

    public Vec3i? getSpawnPos()
    {
        return playerSpawnCoordinate;
    }

    public void setSpawnPos(Vec3i? spawnPos)
    {
        if (spawnPos is (int x, int y, int z))
        {
            playerSpawnCoordinate = new Vec3i(x, y, z);
        }
        else
        {
            playerSpawnCoordinate = null;
        }
    }

    public void incrementStat(StatBase stat)
    {
        increaseStat(stat, 1);
    }

    public virtual void increaseStat(StatBase stat, int amount)
    {
    }

    protected override void jump()
    {
        base.jump();
        increaseStat(Stats.Stats.JumpStat, 1);
    }

    public override void travel(float x, float z)
    {
        double startX = base.X;
        double startY = Y;
        double startZ = base.Z;
        base.travel(x, z);
        updateMovementStat(base.X - startX, Y - startY, base.Z - startZ);
    }

    private void updateMovementStat(double x, double y, double z)
    {
        if (Vehicle == null)
        {
            int distanceScaled;
            if (IsInFluid(Material.Water))
            {
                distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + y * y + z * z) * 100.0F);
                if (distanceScaled > 0)
                {
                    increaseStat(Stats.Stats.DistanceDoveStat, distanceScaled);
                }
            }
            else if (IsInWater())
            {
                distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + z * z) * 100.0F);
                if (distanceScaled > 0)
                {
                    increaseStat(Stats.Stats.DistanceSwumStat, distanceScaled);
                }
            }
            else if (isOnLadder())
            {
                if (y > 0.0D)
                {
                    increaseStat(Stats.Stats.DistanceClimbedStat, (int)MathHelper.Round(y * 100.0D));
                }
            }
            else if (OnGround)
            {
                distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + z * z) * 100.0F);
                if (distanceScaled > 0)
                {
                    increaseStat(Stats.Stats.DistanceWalkedStat, distanceScaled);
                }
            }
            else
            {
                distanceScaled = MathHelper.Round(MathHelper.Sqrt(x * x + z * z) * 100.0F);
                if (distanceScaled > 25)
                {
                    increaseStat(Stats.Stats.DistanceFlownStat, distanceScaled);
                }
            }
        }
    }

    private void increaseRidingMotionStats(double x, double y, double z)
    {
        if (Vehicle is null) return;

        int distanceScaled = (int)Math.Round(Math.Sqrt(x * x + y * y + z * z) * 100.0);

        if (distanceScaled <= 0) return;

        switch (Vehicle)
        {
            case EntityMinecart:
                increaseStat(Stats.Stats.DistanceByMinecartStat, distanceScaled);

                int currentX = MathHelper.Floor(this.X);
                int currentY = MathHelper.Floor(this.Y);
                int currentZ = MathHelper.Floor(this.Z);

                if (startMinecartRidingCoordinate is null)
                {
                    startMinecartRidingCoordinate = new Vec3i(currentX, currentY, currentZ);
                }
                else if (startMinecartRidingCoordinate.Value.SquaredDistanceTo(new Vec3i(currentX, currentY, currentZ)) >= 1_000_000)
                {
                    increaseStat(Achievements.CraftRail, 1);
                }
                break;

            case EntityBoat:
                increaseStat(Stats.Stats.DistanceByBoatStat, distanceScaled);
                break;

            case EntityPig:
                increaseStat(Stats.Stats.DistanceByPigStat, distanceScaled);
                break;
        }
    }

    protected override void OnLanding(float fallDistance)
    {
        if (fallDistance >= 2.0F)
        {
            increaseStat(Stats.Stats.DistanceFallenStat, (int)MathHelper.Round((double)fallDistance * 100.0D));
        }

        base.OnLanding(fallDistance);
    }

    public override void OnKillOther(EntityLiving other)
    {
        if (other is EntityMonster)
        {
            incrementStat(Achievements.KillEnemy);
        }
    }

    public override int getItemStackTextureId(ItemStack stack)
    {
        int textureId = base.getItemStackTextureId(stack);
        if (stack.ItemId == Item.FishingRod.id && fishHook != null)
        {
            textureId = stack.getTextureId() + 16;
        }

        return textureId;
    }

    public override void TickPortalCooldown()
    {
        if (portalCooldown <= 0)
        {
            inTeleportationState = true;
        }
    }

    public virtual void sendChatMessage(string message) { }

    protected internal const float AirFlySpeedMult = 5f;
    protected override float AirSpeed() => GameMode.DisallowFlying ? 0.02f : AirFlySpeedMult * 0.02f;
}
