using BetaSharp.Blocks.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Screens;
using BetaSharp.Screens.Slots;
using BetaSharp.Server;
using BetaSharp.Server.Entities;
using BetaSharp.Server.Network;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

public class ServerPlayerEntity : EntityPlayer, ScreenHandlerListener
{
    private const int MaxChunkPackets = 16;
    private static readonly ILogger s_logger = Log.Instance.For<ServerPlayerEntity>();
    private readonly ItemStack?[] _equipment = [null, null, null, null, null];
    private readonly PlayerChunkSendQueue _pendingChunkUpdates = new();
    private readonly BetaSharpServer _server;
    public readonly HashSet<ChunkPos> ActiveChunks = [];
    public readonly Queue<ChunkPos> PendingChunkUpdates = new();
    private double _chunkStreamingMotionX;
    private double _chunkStreamingMotionZ;
    private int _joinInvulnerabilityTicks = 60;
    private int _lastHealthScore = -99999999;
    private int _screenHandlerSyncId;
    public ServerPlayerInteractionManager InteractionManager;
    public double LastX;
    public double LastZ;
    public bool SkipPacketSlotUpdates;

    public ServerPlayerEntity(BetaSharpServer server, IWorldContext world, string name, ServerPlayerInteractionManager interactionManager) : base(world)
    {
        interactionManager.player = this;
        InteractionManager = interactionManager;
        Vec3i spawnPos = world.Properties.GetSpawnPos();
        int x = spawnPos.X;
        int y = spawnPos.Z;
        int z = spawnPos.Y;
        if (!world.Dimension.HasCeiling)
        {
            if (world.Properties.TerrainType == WorldType.Sky)
            {
                int validityY = world.Reader.GetSpawnPositionValidityY(x, y);
                if (validityY > 0)
                {
                    z = validityY;
                }
            }
            else
            {
                x += Random.NextInt(20) - 10;
                z = world.Reader.GetSpawnPositionValidityY(x, y);
                y += Random.NextInt(20) - 10;
            }
        }

        SetPositionAndAnglesKeepPrevAngles(x + 0.5, z, y + 0.5, 0.0F, 0.0F);
        _server = server;
        StepHeight = 0.0F;
        Name = name;
        StandingEyeHeight = 0.0F;
    }

    public override EntityType Type => EntityRegistry.Player;
    public Dictionary<ChunkPos, long> ChunksTerrainSentToClient { get; } = [];

    public ServerPlayNetworkHandler? NetworkHandler { get; set; }

    public override ItemStack?[] Equipment => _equipment;


    public override float EyeHeight => 1.62F;


    public void onSlotUpdate(ScreenHandler handler, int slot, ItemStack? stack)
    {
        if (handler.GetSlot(slot) is CraftingResultSlot) return;

        if (!SkipPacketSlotUpdates)
        {
            NetworkHandler?.SendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(handler.SyncId, slot, stack));
        }
    }


    public void onContentsUpdate(ScreenHandler handler, List<ItemStack> stacks)
    {
        NetworkHandler?.SendPacket(InventoryS2CPacket.Get(handler.SyncId, stacks));
        NetworkHandler?.SendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(-1, -1, Inventory.GetCursorStack()));
    }

    public void onPropertyUpdate(ScreenHandler handler, int syncId, int trackedValue) => NetworkHandler?.SendPacket(ScreenHandlerPropertyUpdateS2CPacket.Get(handler.SyncId, syncId, trackedValue));


    public override void SetWorld(IWorldContext world)
    {
        base.SetWorld(world);
        InteractionManager = new ServerPlayerInteractionManager((ServerWorld)world)
        {
            player = this
        };
    }

    public void initScreenHandler() => CurrentScreenHandler?.AddListener(this);

    protected override void resetEyeHeight() => StandingEyeHeight = 0.0F;

    public override void Tick()
    {
        TickSleep();

        InteractionManager.update();
        _joinInvulnerabilityTicks--;
        CurrentScreenHandler?.SendContentUpdates();

        for (int i = 0; i < 5; i++)
        {
            ItemStack? itemStack = getEquipment(i);
            if (itemStack == _equipment[i]) continue;

            _server.getEntityTracker(DimensionId).sendToListeners(this, EntityEquipmentUpdateS2CPacket.Get(ID, i, itemStack));
            _equipment[i] = itemStack;
        }
    }

    private ItemStack? getEquipment(int slot) => slot == 0 ? Inventory.ItemInHand : Inventory.Armor[slot - 1];

    public override bool Damage(Entity? damageSource, int amount)
    {
        if (_joinInvulnerabilityTicks > 0) return false;
        if (_server.pvpEnabled) return base.Damage(damageSource, amount);

        switch (damageSource)
        {
            case EntityPlayer:
            case EntityArrow { Owner: EntityPlayer }:
                return false;
        }

        return base.Damage(damageSource, amount);
    }

    protected override bool isPvpEnabled() => _server.pvpEnabled;

    public void PlayerTick(bool shouldSendChunkUpdates)
    {
        GenericTick();
        PlayerTickPostGeneric(shouldSendChunkUpdates);
    }

    public void IdleTick()
    {
        base.BaseTick();
        AfterLivingTickCosmetics();
        PickupAndInventorySubtick();
        CollideWithPickupEntities();
        PlayerTickPostGeneric(false);
    }

    private void PlayerTickPostGeneric(bool shouldSendChunkUpdates)
    {
        for (int slotIndex = 0; slotIndex < Inventory.Size; slotIndex++)
        {
            ItemStack? itemStack = Inventory.GetStack(slotIndex);
            if (NetworkHandler != null && (itemStack == null || !Item.ITEMS[itemStack.ItemId]!.isNetworkSynced() || NetworkHandler?.getBlockDataSendQueueSize() > 2))
            {
                continue;
            }

            if (itemStack == null)
            {
                continue;
            }

            Packet? packet = ((NetworkSyncedItem)Item.ITEMS[itemStack.ItemId]!).getUpdatePacket(itemStack, World, this);
            if (packet != null)
            {
                NetworkHandler?.SendPacket(packet);
            }
        }

        if (shouldSendChunkUpdates)
        {
            FlushPendingChunkUpdates();
        }

        if (InTeleportationState)
        {
            if (_server.config.GetAllowNether(true))
            {
                if (CurrentScreenHandler != PlayerScreenHandler)
                {
                    closeHandledScreen();
                }

                if (Vehicle != null)
                {
                    SetVehicle(Vehicle);
                }
                else
                {
                    ChangeDimensionCooldown += 0.0125F;
                    if (ChangeDimensionCooldown >= 1.0F)
                    {
                        ChangeDimensionCooldown = 1.0F;
                        PortalCooldown = 10;
                        _server.playerManager.changePlayerDimension(this);
                    }
                }

                InTeleportationState = false;
            }
        }
        else
        {
            if (ChangeDimensionCooldown > 0.0F)
            {
                ChangeDimensionCooldown -= 0.05F;
            }

            if (ChangeDimensionCooldown < 0.0F)
            {
                ChangeDimensionCooldown = 0.0F;
            }
        }

        if (PortalCooldown > 0)
        {
            PortalCooldown--;
        }

        if (Health == _lastHealthScore)
        {
            return;
        }

        NetworkHandler?.SendPacket(HealthUpdateS2CPacket.Get(Health));
        _lastHealthScore = Health;
    }

    private bool CanSendMoreChunkData() => NetworkHandler != null && NetworkHandler.getBlockDataSendQueueSize() < MaxChunkPackets;

    public void ResetChunkStreamingState()
    {
        _chunkStreamingMotionX = 0.0;
        _chunkStreamingMotionZ = 0.0;
        _pendingChunkUpdates.Clear();
    }

    public void UpdateChunkStreamingMotion(double motionX, double motionZ)
    {
        _chunkStreamingMotionX = motionX;
        _chunkStreamingMotionZ = motionZ;
        _pendingChunkUpdates.ReprioritizeAll(this);
    }

    public void ScheduleChunkSend(ChunkPos chunkPos) => _pendingChunkUpdates.EnqueueOrPromote(this, chunkPos);

    public void CancelChunkSend(ChunkPos chunkPos) => _pendingChunkUpdates.Remove(chunkPos);

    public void FlushPendingChunkUpdates()
    {
        if (_pendingChunkUpdates.Count == 0)
        {
            return;
        }

        ServerWorld world = _server.getWorld(DimensionId);
        while (CanSendMoreChunkData() && _pendingChunkUpdates.TryDequeue(out ChunkPos chunkPos))
        {
            if (!ActiveChunks.Contains(chunkPos)) continue;

            SendChunkData(world, chunkPos);
            ChunksTerrainSentToClient[chunkPos] = Environment.TickCount64;
            SendBlockEntityUpdates(world, chunkPos);
            _server.getEntityTracker(DimensionId).updateListenerForChunk(this, chunkPos.X, chunkPos.Z);
        }
    }

    internal ChunkPriority GetChunkPriority(ChunkPos chunkPos, long sequence)
    {
        int playerChunkX = (int)X >> 4;
        int playerChunkZ = (int)Z >> 4;
        int deltaX = chunkPos.X - playerChunkX;
        int deltaZ = chunkPos.Z - playerChunkZ;
        int ring = Math.Max(Math.Abs(deltaX), Math.Abs(deltaZ));

        double directionPenalty = 0.0;
        double motionLength = Math.Sqrt(_chunkStreamingMotionX * _chunkStreamingMotionX + _chunkStreamingMotionZ * _chunkStreamingMotionZ);
        if (motionLength > 0.0)
        {
            directionPenalty = -(deltaX * _chunkStreamingMotionX + deltaZ * _chunkStreamingMotionZ) / motionLength;
        }

        return new ChunkPriority(ring, directionPenalty, sequence);
    }

    private void SendChunkData(IWorldContext world, ChunkPos chunkPos)
    {
        int worldX = chunkPos.X * 16;
        int worldZ = chunkPos.Z * 16;
        NetworkHandler?.SendPacket(ChunkDataS2CPacket.Get(worldX, 0, worldZ, 16, ChuckFormat.WorldHeight, 16, world));
    }

    private void SendBlockEntityUpdates(IWorldContext world, ChunkPos chunkPos)
    {
        int startX = chunkPos.X * 16;
        int startZ = chunkPos.Z * 16;
        int endX = startX + 16;
        int endZ = startZ + 16;

        List<BlockEntity> blockEntities = world.Entities.GetBlockEntities(startX, 0, startZ, endX, ChuckFormat.WorldHeight, endZ);
        foreach (BlockEntity blockEntity in blockEntities)
        {
            updateBlockEntity(blockEntity);
        }
    }

    private void updateBlockEntity(BlockEntity? blockEntity)
    {
        Packet? packet = blockEntity?.createUpdatePacket();
        if (packet != null)
        {
            NetworkHandler?.SendPacket(packet);
        }
    }

    public override void sendPickup(Entity item, int count)
    {
        if (!GameMode.CanPickup) return;

        if (!item.Dead)
        {
            EntityTracker et = _server.getEntityTracker(DimensionId);
            switch (item)
            {
                case EntityItem:
                case EntityArrow:
                    et.sendToListeners(item, ItemPickupAnimationS2CPacket.Get(item.ID, ID));
                    break;
            }
        }

        base.sendPickup(item, count);
        CurrentScreenHandler?.SendContentUpdates();
    }

    public override void SwingHand()
    {
        if (HandSwinging) return;

        HandSwingTicks = -1;
        HandSwinging = true;
        EntityTracker et = _server.getEntityTracker(DimensionId);
        et.sendToListeners(this, EntityAnimationPacket.Get(this, EntityAnimationPacket.EntityAnimation.SwingHand));
    }

    public override SleepAttemptResult TrySleep(int x, int y, int z)
    {
        SleepAttemptResult sleepAttemptResult = base.TrySleep(x, y, z);
        if (sleepAttemptResult != SleepAttemptResult.OK)
        {
            return sleepAttemptResult;
        }

        EntityTracker et = _server.getEntityTracker(DimensionId);
        PlayerSleepUpdateS2CPacket packet = PlayerSleepUpdateS2CPacket.Get(this, 0, x, y, z);
        et.sendToAround(this, packet);
        NetworkHandler?.teleport(x, y, z, Yaw, Pitch);

        return sleepAttemptResult;
    }

    public override void WakeUp(bool resetSleepTimer, bool updateSleepingPlayers, bool setSpawnPos)
    {
        if (IsSleeping)
        {
            EntityTracker et = _server.getEntityTracker(DimensionId);
            et.sendToAround(this, EntityAnimationPacket.Get(this, EntityAnimationPacket.EntityAnimation.WakeUp));
        }

        base.WakeUp(resetSleepTimer, updateSleepingPlayers, setSpawnPos);
        NetworkHandler?.teleport(X, Y, Z, Yaw, Pitch);
    }


    public override void SetVehicle(Entity? entity)
    {
        base.SetVehicle(entity);
        NetworkHandler?.SendPacket(EntityVehicleSetS2CPacket.Get(this, Vehicle));
        NetworkHandler?.teleport(X, Y, Z, Yaw, Pitch);
    }


    protected override void Fall(double heightDifference, bool onGround)
    {
    }

    public void handleFall(double heightDifference, bool onGround) => base.Fall(heightDifference, onGround);

    private void incrementScreenHandlerSyncId() => _screenHandlerSyncId = _screenHandlerSyncId % 100 + 1;


    public override void openCraftingScreen(int x, int y, int z)
    {
        incrementScreenHandlerSyncId();
        NetworkHandler?.SendPacket(OpenScreenS2CPacket.Get(_screenHandlerSyncId, 1, "Crafting", 9));
        CurrentScreenHandler = new CraftingScreenHandler(Inventory, World, x, y, z);
        CurrentScreenHandler.SyncId = _screenHandlerSyncId;
        CurrentScreenHandler.AddListener(this);
    }


    public override void openChestScreen(IInventory inventory)
    {
        incrementScreenHandlerSyncId();
        NetworkHandler?.SendPacket(OpenScreenS2CPacket.Get(_screenHandlerSyncId, 0, inventory.Name, inventory.Size));
        CurrentScreenHandler = new GenericContainerScreenHandler(Inventory, inventory);
        CurrentScreenHandler.SyncId = _screenHandlerSyncId;
        CurrentScreenHandler.AddListener(this);
    }


    public override void openFurnaceScreen(BlockEntityFurnace furnace)
    {
        incrementScreenHandlerSyncId();
        NetworkHandler?.SendPacket(OpenScreenS2CPacket.Get(_screenHandlerSyncId, 2, furnace.Name, furnace.Size));
        CurrentScreenHandler = new FurnaceScreenHandler(Inventory, furnace);
        CurrentScreenHandler.SyncId = _screenHandlerSyncId;
        CurrentScreenHandler.AddListener(this);
    }


    public override void openDispenserScreen(BlockEntityDispenser dispenser)
    {
        incrementScreenHandlerSyncId();
        NetworkHandler?.SendPacket(OpenScreenS2CPacket.Get(_screenHandlerSyncId, 3, dispenser.Name, dispenser.Size));
        CurrentScreenHandler = new DispenserScreenHandler(Inventory, dispenser);
        CurrentScreenHandler.SyncId = _screenHandlerSyncId;
        CurrentScreenHandler.AddListener(this);
    }

    public void onContentsUpdate(ScreenHandler screenHandler) => onContentsUpdate(screenHandler, screenHandler.GetStacks());

    public override void OnCursorStackChanged(ItemStack? stack)
    {
    }

    public override void closeHandledScreen()
    {
        NetworkHandler?.SendPacket(CloseScreenS2CPacket.Get(CurrentScreenHandler.SyncId));
        onHandledScreenClosed();
    }

    public void updateCursorStack()
    {
        if (!SkipPacketSlotUpdates)
        {
            NetworkHandler?.SendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(-1, -1, Inventory.GetCursorStack()));
        }
    }

    public void onHandledScreenClosed()
    {
        CurrentScreenHandler.onClosed(this);
        CurrentScreenHandler = PlayerScreenHandler;
    }

    public void updateInput(float sidewaysSpeed, float forwardSpeed, bool jumping, bool sneaking, float pitch, float yaw)
    {
        SidewaysSpeed = sidewaysSpeed;
        ForwardSpeed = forwardSpeed;
        Jumping = jumping;
        SetSneaking(sneaking);
        Pitch = pitch;
        Yaw = yaw;
    }

    public void updateInput(PlayerInputC2SPacket packet)
    {
        if (GameMode is { CanWalk: false, DisallowFlying: true })
        {
            SidewaysSpeed = packet.getSideways();
            ForwardSpeed = packet.getForward();
        }
        else
        {
            SidewaysSpeed = 0;
            ForwardSpeed = 0;
        }

        Jumping = packet.isJumping();
        SetSneaking(packet.isSneaking());
        Pitch = packet.getPitch();
        Yaw = packet.getYaw();
    }


    public override void IncreaseStat(StatBase stat, int amount)
    {
        if (stat is not { LocalOnly: false }) return;

        if (stat.IsAchievement())
        {
            s_logger.LogInformation("Player {PlayerName} unlocked {AchievementName}", Name, stat.StatName);
        }

        while (amount > 100)
        {
            NetworkHandler?.SendPacket(IncreaseStatS2CPacket.Get(stat.Id, 100));
            amount -= 100;
        }

        NetworkHandler?.SendPacket(IncreaseStatS2CPacket.Get(stat.Id, amount));
    }

    public void onDisconnect()
    {
        if (Vehicle != null) SetVehicle(Vehicle);

        Passenger?.SetVehicle(this);

        if (Sleeping)
        {
            WakeUp(true, false, false);
        }
    }

    public void markHealthDirty() => _lastHealthScore = -99999999;

    public override void SendMessage(string message)
    {
        TranslationStorage ts = TranslationStorage.Instance;
        string translatedMessage = ts.TranslateKey(message);
        NetworkHandler?.SendPacket(ChatMessagePacket.Get(translatedMessage));
    }

    //client only
    public override void Spawn() => throw new NotImplementedException();
}
