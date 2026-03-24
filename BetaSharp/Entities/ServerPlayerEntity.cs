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
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Entities;

public class ServerPlayerEntity : EntityPlayer, ScreenHandlerListener
{
    private static readonly ILogger s_logger = Log.Instance.For<ServerPlayerEntity>();
    public override EntityType Type => EntityRegistry.Player;
    private const int MaxChunkPackets = 16;
    private readonly ItemStack?[] equipment = [null, null, null, null, null];
    public HashSet<ChunkPos> activeChunks = new();
    public Dictionary<ChunkPos, long> ChunksTerrainSentToClient { get; } = [];
    public ServerPlayerInteractionManager interactionManager;
    private int joinInvulnerabilityTicks = 60;
    private int lastHealthScore = -99999999;
    public double lastX;
    public double lastZ;

    public ServerPlayNetworkHandler networkHandler;
    public Queue<ChunkPos> PendingChunkUpdates = new();
    private int screenHandlerSyncId;
    public BetaSharpServer server;
    public bool skipPacketSlotUpdates;

    public ServerPlayerEntity(BetaSharpServer server, IWorldContext world, string name, ServerPlayerInteractionManager interactionManager) : base(world)
    {
        interactionManager.player = this;
        this.interactionManager = interactionManager;
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
                    z = validityY;
            }
            else
            {
                x += random.NextInt(20) - 10;
                z = world.Reader.GetSpawnPositionValidityY(x, y);
                y += random.NextInt(20) - 10;
            }
        }

        setPositionAndAnglesKeepPrevAngles(x + 0.5, z, y + 0.5, 0.0F, 0.0F);
        this.server = server;
        stepHeight = 0.0F;
        this.name = name;
        standingEyeHeight = 0.0F;
    }


    public void onSlotUpdate(ScreenHandler handler, int slot, ItemStack? stack)
    {
        if (handler.GetSlot(slot) is not CraftingResultSlot)
        {
            if (!skipPacketSlotUpdates)
            {
                networkHandler.sendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(handler.SyncId, slot, stack));
            }
        }
    }


    public void onContentsUpdate(ScreenHandler handler, List<ItemStack> stacks)
    {
        networkHandler.sendPacket(InventoryS2CPacket.Get(handler.SyncId, stacks));
        networkHandler.sendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(-1, -1, inventory.getCursorStack()));
    }

    public void onPropertyUpdate(ScreenHandler handler, int syncId, int trackedValue) => networkHandler.sendPacket(ScreenHandlerPropertyUpdateS2CPacket.Get(handler.SyncId, syncId, trackedValue));


    public override void setWorld(IWorldContext world)
    {
        base.setWorld(world);
        interactionManager = new ServerPlayerInteractionManager((ServerWorld)world);
        interactionManager.player = this;
    }

    public void initScreenHandler() => currentScreenHandler.AddListener(this);

    public override ItemStack[] getEquipment() => equipment;

    protected override void resetEyeHeight() => standingEyeHeight = 0.0F;


    public override float getEyeHeight() => 1.62F;

    public override void tick()
    {
        TickSleep();

        interactionManager.update();
        joinInvulnerabilityTicks--;
        currentScreenHandler.SendContentUpdates();

        for (int i = 0; i < 5; i++)
        {
            ItemStack itemStack = getEquipment(i);
            if (itemStack != equipment[i])
            {
                server.getEntityTracker(dimensionId).sendToListeners(this, EntityEquipmentUpdateS2CPacket.Get(id, i, itemStack));
                equipment[i] = itemStack;
            }
        }
    }

    public ItemStack getEquipment(int slot) => slot == 0 ? inventory.getSelectedItem() : inventory.armor[slot - 1];

    public override bool damage(Entity damageSource, int amount)
    {
        if (joinInvulnerabilityTicks > 0)
        {
            return false;
        }

        if (!server.pvpEnabled)
        {
            if (damageSource is EntityPlayer)
            {
                return false;
            }

            if (damageSource is EntityArrow arrow)
            {
                if (arrow.owner is EntityPlayer)
                {
                    return false;
                }
            }
        }

        return base.damage(damageSource, amount);
    }

    protected override bool isPvpEnabled() => server.pvpEnabled;

    public void playerTick(bool shouldSendChunkUpdates)
    {
        GenericTick();

        for (int slotIndex = 0; slotIndex < inventory.size(); slotIndex++)
        {
            ItemStack itemStack = inventory.getStack(slotIndex);
            if (itemStack != null && Item.ITEMS[itemStack.itemId].isNetworkSynced() && networkHandler.getBlockDataSendQueueSize() <= 2)
            {
                Packet packet = ((NetworkSyncedItem)Item.ITEMS[itemStack.itemId]).getUpdatePacket(itemStack, world, this);
                if (packet != null)
                {
                    networkHandler.sendPacket(packet);
                }
            }
        }

        if (shouldSendChunkUpdates)
        {
            while (CanSendMoreChunkData() && PendingChunkUpdates.TryDequeue(out ChunkPos chunkPos))
            {
                ServerWorld world = server.getWorld(dimensionId);
                if (!activeChunks.Contains(chunkPos))
                {
                    continue;
                }

                SendChunkData(world, chunkPos);
                ChunksTerrainSentToClient[chunkPos] = Environment.TickCount64;
                SendBlockEntityUpdates(world, chunkPos);
            }
        }

        if (inTeleportationState)
        {
            if (server.config.GetAllowNether(true))
            {
                if (currentScreenHandler != playerScreenHandler)
                {
                    closeHandledScreen();
                }

                if (vehicle != null)
                {
                    setVehicle(vehicle);
                }
                else
                {
                    changeDimensionCooldown += 0.0125F;
                    if (changeDimensionCooldown >= 1.0F)
                    {
                        changeDimensionCooldown = 1.0F;
                        portalCooldown = 10;
                        server.playerManager.changePlayerDimension(this);
                    }
                }

                inTeleportationState = false;
            }
        }
        else
        {
            if (changeDimensionCooldown > 0.0F)
            {
                changeDimensionCooldown -= 0.05F;
            }

            if (changeDimensionCooldown < 0.0F)
            {
                changeDimensionCooldown = 0.0F;
            }
        }

        if (portalCooldown > 0)
        {
            portalCooldown--;
        }

        if (health != lastHealthScore)
        {
            networkHandler.sendPacket(HealthUpdateS2CPacket.Get(health));
            lastHealthScore = health;
        }
    }

    private bool CanSendMoreChunkData() => networkHandler.getBlockDataSendQueueSize() < MaxChunkPackets;

    private void SendChunkData(IWorldContext world, ChunkPos chunkPos)
    {
        int worldX = chunkPos.X * 16;
        int worldZ = chunkPos.Z * 16;
        networkHandler.sendPacket(ChunkDataS2CPacket.Get(worldX, 0, worldZ, 16, 128, 16, world));
    }

    private void SendBlockEntityUpdates(IWorldContext world, ChunkPos chunkPos)
    {
        int startX = chunkPos.X * 16;
        int startZ = chunkPos.Z * 16;
        int endX = startX + 16;
        int endZ = startZ + 16;

        var blockEntities = world.Entities.GetBlockEntities(startX, 0, startZ, endX, 128, endZ);
        foreach (BlockEntity blockEntity in blockEntities)
        {
            updateBlockEntity(blockEntity);
        }
    }

    private void updateBlockEntity(BlockEntity blockentity)
    {
        if (blockentity != null)
        {
            Packet packet = blockentity.createUpdatePacket();
            if (packet != null)
            {
                networkHandler.sendPacket(packet);
            }
        }
    }

    public override void sendPickup(Entity item, int count)
    {
        if (!item.dead)
        {
            EntityTracker et = server.getEntityTracker(dimensionId);
            if (item is EntityItem)
            {
                et.sendToListeners(item, ItemPickupAnimationS2CPacket.Get(item.id, id));
            }

            if (item is EntityArrow)
            {
                et.sendToListeners(item, ItemPickupAnimationS2CPacket.Get(item.id, id));
            }
        }

        base.sendPickup(item, count);
        currentScreenHandler.SendContentUpdates();
    }

    public override void swingHand()
    {
        if (!handSwinging)
        {
            handSwingTicks = -1;
            handSwinging = true;
            EntityTracker et = server.getEntityTracker(dimensionId);
            et.sendToListeners(this, EntityAnimationPacket.Get(this, EntityAnimationPacket.EntityAnimation.SwingHand));
        }
    }

    public override SleepAttemptResult trySleep(int x, int y, int z)
    {
        SleepAttemptResult sleepAttemptResult = base.trySleep(x, y, z);
        if (sleepAttemptResult == SleepAttemptResult.OK)
        {
            EntityTracker et = server.getEntityTracker(dimensionId);
            PlayerSleepUpdateS2CPacket packet = PlayerSleepUpdateS2CPacket.Get(this, 0, x, y, z);
            et.sendToAround(this, packet);
            networkHandler.teleport(x, y, z, yaw, pitch);
        }

        return sleepAttemptResult;
    }

    public override void wakeUp(bool resetSleepTimer, bool updateSleepingPlayers, bool setSpawnPos)
    {
        if (isSleeping())
        {
            EntityTracker et = server.getEntityTracker(dimensionId);
            et.sendToAround(this, EntityAnimationPacket.Get(this, EntityAnimationPacket.EntityAnimation.WakeUp));
        }

        base.wakeUp(resetSleepTimer, updateSleepingPlayers, setSpawnPos);
        if (networkHandler != null)
        {
            networkHandler.teleport(x, y, z, yaw, pitch);
        }
    }


    public override void setVehicle(Entity entity)
    {
        base.setVehicle(entity);
        networkHandler.sendPacket(EntityVehicleSetS2CPacket.Get(this, vehicle));
        networkHandler.teleport(x, y, z, yaw, pitch);
    }


    protected override void fall(double heightDifference, bool onGround)
    {
    }

    public void handleFall(double heightDifference, bool onGround) => base.fall(heightDifference, onGround);

    private void incrementScreenHandlerSyncId() => screenHandlerSyncId = screenHandlerSyncId % 100 + 1;


    public override void openCraftingScreen(int x, int y, int z)
    {
        incrementScreenHandlerSyncId();
        networkHandler.sendPacket(OpenScreenS2CPacket.Get(screenHandlerSyncId, 1, "Crafting", 9));
        currentScreenHandler = new CraftingScreenHandler(inventory, world, x, y, z);
        currentScreenHandler.SyncId = screenHandlerSyncId;
        currentScreenHandler.AddListener(this);
    }


    public override void openChestScreen(IInventory inventory)
    {
        incrementScreenHandlerSyncId();
        networkHandler.sendPacket(OpenScreenS2CPacket.Get(screenHandlerSyncId, 0, inventory.getName(), inventory.size()));
        currentScreenHandler = new GenericContainerScreenHandler(this.inventory, inventory);
        currentScreenHandler.SyncId = screenHandlerSyncId;
        currentScreenHandler.AddListener(this);
    }


    public override void openFurnaceScreen(BlockEntityFurnace furnace)
    {
        incrementScreenHandlerSyncId();
        networkHandler.sendPacket(OpenScreenS2CPacket.Get(screenHandlerSyncId, 2, furnace.getName(), furnace.size()));
        currentScreenHandler = new FurnaceScreenHandler(inventory, furnace);
        currentScreenHandler.SyncId = screenHandlerSyncId;
        currentScreenHandler.AddListener(this);
    }


    public override void openDispenserScreen(BlockEntityDispenser dispenser)
    {
        incrementScreenHandlerSyncId();
        networkHandler.sendPacket(OpenScreenS2CPacket.Get(screenHandlerSyncId, 3, dispenser.getName(), dispenser.size()));
        currentScreenHandler = new DispenserScreenHandler(inventory, dispenser);
        currentScreenHandler.SyncId = screenHandlerSyncId;
        currentScreenHandler.AddListener(this);
    }

    public void onContentsUpdate(ScreenHandler screenHandler) => onContentsUpdate(screenHandler, screenHandler.GetStacks());

    public override void onCursorStackChanged(ItemStack? stack)
    {
    }

    public override void closeHandledScreen()
    {
        networkHandler.sendPacket(CloseScreenS2CPacket.Get(currentScreenHandler.SyncId));
        onHandledScreenClosed();
    }

    public void updateCursorStack()
    {
        if (!skipPacketSlotUpdates)
        {
            networkHandler.sendPacket(ScreenHandlerSlotUpdateS2CPacket.Get(-1, -1, inventory.getCursorStack()));
        }
    }

    public void onHandledScreenClosed()
    {
        currentScreenHandler.onClosed(this);
        currentScreenHandler = playerScreenHandler;
    }

    public void updateInput(float sidewaysSpeed, float forwardSpeed, bool jumping, bool sneaking, float pitch, float yaw)
    {
        this.sidewaysSpeed = sidewaysSpeed;
        this.forwardSpeed = forwardSpeed;
        this.jumping = jumping;
        setSneaking(sneaking);
        this.pitch = pitch;
        this.yaw = yaw;
    }

    public void updateInput(PlayerInputC2SPacket packet)
    {
        sidewaysSpeed = packet.getSideways();
        forwardSpeed = packet.getForward();
        jumping = packet.isJumping();
        setSneaking(packet.isSneaking());
        pitch = packet.getPitch();
        yaw = packet.getYaw();
    }


    public override void increaseStat(StatBase stat, int amount)
    {
        if (stat != null)
        {
            if (!stat.LocalOnly)
            {
                if (stat.IsAchievement())
                {
                    s_logger.LogInformation("Player {PlayerName} unlocked {AchievementName}", name, stat.StatName);
                }

                while (amount > 100)
                {
                    networkHandler.sendPacket(IncreaseStatS2CPacket.Get(stat.Id, 100));
                    amount -= 100;
                }

                networkHandler.sendPacket(IncreaseStatS2CPacket.Get(stat.Id, amount));
            }
        }
    }

    public void onDisconnect()
    {
        if (vehicle != null)
        {
            setVehicle(vehicle);
        }

        if (passenger != null)
        {
            passenger.setVehicle(this);
        }

        if (sleeping)
        {
            wakeUp(true, false, false);
        }
    }

    public void markHealthDirty() => lastHealthScore = -99999999;

    public override void sendMessage(string message)
    {
        TranslationStorage ts = TranslationStorage.Instance;
        string translatedMessage = ts.TranslateKey(message);
        networkHandler.sendPacket(ChatMessagePacket.Get(translatedMessage));
    }

    public override void spawn() => throw
        //client only
        new NotImplementedException();
}
