using BetaSharp.Blocks.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Network.Packets;
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
using java.util;

namespace BetaSharp.Entities;

public class ServerPlayerEntity : EntityPlayer, ScreenHandlerListener
{
    private const int MaxChunkPackets = 16;

    public ServerPlayNetworkHandler networkHandler;
    public MinecraftServer server;
    public ServerPlayerInteractionManager interactionManager;
    public double lastX;
    public double lastZ;
    public Queue<ChunkPos> PendingChunkUpdates = new();
    public HashSet<ChunkPos> activeChunks = new();
    private int lastHealthScore = -99999999;
    private int joinInvulnerabilityTicks = 60;
    private ItemStack[] equipment = [null, null, null, null, null];
    private int screenHandlerSyncId;
    public bool skipPacketSlotUpdates;

    public ServerPlayerEntity(MinecraftServer server, World world, String name, ServerPlayerInteractionManager interactionManager) : base(world)
    {
        interactionManager.Player = this;
        this.interactionManager = interactionManager;
        Vec3i spawnPos = world.getSpawnPos();
        int x = spawnPos.X;
        int y = spawnPos.Z;
        int z = spawnPos.Y;
        if (!world.dimension.HasCeiling)
        {
            x += random.NextInt(20) - 10;
            z = world.getSpawnPositionValidityY(x, y);
            y += random.NextInt(20) - 10;
        }

        setPositionAndAnglesKeepPrevAngles(x + 0.5, z, y + 0.5, 0.0F, 0.0F);
        this.server = server;
        stepHeight = 0.0F;
        this.name = name;
        standingEyeHeight = 0.0F;
    }


    public override void setWorld(World world)
    {
        base.setWorld(world);
        interactionManager = new ServerPlayerInteractionManager((ServerWorld)world);
        interactionManager.Player = this;
    }

    public void initScreenHandler()
    {
        currentScreenHandler.addListener(this);
    }

    public override ItemStack[] getEquipment()
    {
        return equipment;
    }

    protected override void resetEyeHeight()
    {
        standingEyeHeight = 0.0F;
    }


    public override float getEyeHeight()
    {
        return 1.62F;
    }

    public override void tick()
    {
        interactionManager.Update();
        joinInvulnerabilityTicks--;
        currentScreenHandler.sendContentUpdates();

        for (int i = 0; i < 5; i++)
        {
            ItemStack itemStack = getEquipment(i);
            if (itemStack != equipment[i])
            {
                server.GetEntityTracker(dimensionId).SendToListeners(this, new EntityEquipmentUpdateS2CPacket(id, i, itemStack));
                equipment[i] = itemStack;
            }
        }
    }

    public ItemStack getEquipment(int slot)
    {
        return slot == 0 ? inventory.getSelectedItem() : inventory.armor[slot - 1];
    }

    public override bool damage(Entity damageSource, int amount)
    {
        if (joinInvulnerabilityTicks > 0)
        {
            return false;
        }
        else
        {
            if (!server.PvpEnabled)
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
    }

    protected override bool isPvpEnabled()
    {
        return server.PvpEnabled;
    }

    public void playerTick(bool shouldSendChunkUpdates)
    {
        base.tick();

        for (int slotIndex = 0; slotIndex < inventory.size(); slotIndex++)
        {
            ItemStack itemStack = inventory.getStack(slotIndex);
            if (itemStack != null && Item.ITEMS[itemStack.itemId].isNetworkSynced() && networkHandler.GetBlockDataSendQueueSize() <= 2)
            {
                Packet packet = ((NetworkSyncedItem)Item.ITEMS[itemStack.itemId]).getUpdatePacket(itemStack, world, this);
                if (packet != null)
                {
                    networkHandler.SendPacket(packet);
                }
            }
        }

        if (shouldSendChunkUpdates)
        {
            while (CanSendMoreChunkData() && PendingChunkUpdates.TryDequeue(out ChunkPos chunkPos))
            {
                ServerWorld world = server.GetWorld(dimensionId);
                if (!activeChunks.Contains(chunkPos)) continue;
                if (!world.chunkCache.GetChunk(chunkPos.X, chunkPos.Z).TerrainPopulated) continue;
                SendChunkData(world, chunkPos);
                SendBlockEntityUpdates(world, chunkPos);
            }
        }

        if (inTeleportationState)
        {
            if (server.Config.GetAllowNether(true))
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
                        server.PlayerManager.ChangePlayerDimension(this);
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
            networkHandler.SendPacket(new HealthUpdateS2CPacket(health));
            lastHealthScore = health;
        }
    }

    private bool CanSendMoreChunkData()
    {
        return networkHandler.GetBlockDataSendQueueSize() < MaxChunkPackets;
    }

    private void SendChunkData(ServerWorld world, ChunkPos chunkPos)
    {
        int worldX = chunkPos.X * 16;
        int worldZ = chunkPos.Z * 16;
        networkHandler.SendPacket(new ChunkDataS2CPacket(worldX, 0, worldZ, 16, 128, 16, world));
    }

    private void SendBlockEntityUpdates(ServerWorld world, ChunkPos chunkPos)
    {
        int startX = chunkPos.X * 16;
        int startZ = chunkPos.Z * 16;
        int endX = startX + 16;
        int endZ = startZ + 16;

        var blockEntities = world.getBlockEntities(startX, 0, startZ, endX, 128, endZ);
        foreach (BlockEntity blockEntity in blockEntities)
        {
            UpdateBlockEntity(blockEntity);
        }
    }

    private void UpdateBlockEntity(BlockEntity blockentity)
    {
        if (blockentity != null)
        {
            Packet packet = blockentity.createUpdatePacket();
            if (packet != null)
            {
                networkHandler.SendPacket(packet);
            }
        }
    }

    public override void sendPickup(Entity item, int count)
    {
        if (!item.dead)
        {
            EntityTracker et = server.GetEntityTracker(dimensionId);
            if (item is EntityItem)
            {
                et.SendToListeners(item, new ItemPickupAnimationS2CPacket(item.id, id));
            }

            if (item is EntityArrow)
            {
                et.SendToListeners(item, new ItemPickupAnimationS2CPacket(item.id, id));
            }
        }

        base.sendPickup(item, count);
        currentScreenHandler.sendContentUpdates();
    }

    public override void swingHand()
    {
        if (!handSwinging)
        {
            handSwingTicks = -1;
            handSwinging = true;
            EntityTracker et = server.GetEntityTracker(dimensionId);
            et.SendToListeners(this, new EntityAnimationPacket(this, 1));
        }
    }

    public override SleepAttemptResult trySleep(int x, int y, int z)
    {
        SleepAttemptResult sleepAttemptResult = base.trySleep(x, y, z);
        if (sleepAttemptResult == SleepAttemptResult.OK)
        {
            EntityTracker et = server.GetEntityTracker(dimensionId);
            PlayerSleepUpdateS2CPacket packet = new(this, 0, x, y, z);
            et.SendToListeners(this, packet);
            networkHandler.Teleport(x, y, z, yaw, pitch);
            networkHandler.SendPacket(packet);
        }

        return sleepAttemptResult;
    }

    public override void wakeUp(bool resetSleepTimer, bool updateSleepingPlayers, bool setSpawnPos)
    {
        if (isSleeping())
        {
            EntityTracker et = server.GetEntityTracker(dimensionId);
            et.SendToAround(this, new EntityAnimationPacket(this, 3));
        }

        base.wakeUp(resetSleepTimer, updateSleepingPlayers, setSpawnPos);
        if (networkHandler != null)
        {
            networkHandler.Teleport(x, y, z, yaw, pitch);
        }
    }


    public override void setVehicle(Entity entity)
    {
        base.setVehicle(entity);
        networkHandler.SendPacket(new EntityVehicleSetS2CPacket(this, vehicle));
        networkHandler.Teleport(x, y, z, yaw, pitch);
    }


    protected override void fall(double heightDifference, bool onGround)
    {
    }

    public void handleFall(double heightDifference, bool onGround)
    {
        base.fall(heightDifference, onGround);
    }

    private void incrementScreenHandlerSyncId()
    {
        screenHandlerSyncId = screenHandlerSyncId % 100 + 1;
    }


    public override void openCraftingScreen(int x, int y, int z)
    {
        incrementScreenHandlerSyncId();
        networkHandler.SendPacket(new OpenScreenS2CPacket(screenHandlerSyncId, 1, "Crafting", 9));
        currentScreenHandler = new CraftingScreenHandler(inventory, world, x, y, z);
        currentScreenHandler.syncId = screenHandlerSyncId;
        currentScreenHandler.addListener(this);
    }


    public override void openChestScreen(IInventory inventory)
    {
        incrementScreenHandlerSyncId();
        networkHandler.SendPacket(new OpenScreenS2CPacket(screenHandlerSyncId, 0, inventory.getName(), inventory.size()));
        currentScreenHandler = new GenericContainerScreenHandler(this.inventory, inventory);
        currentScreenHandler.syncId = screenHandlerSyncId;
        currentScreenHandler.addListener(this);
    }


    public override void openFurnaceScreen(BlockEntityFurnace furnace)
    {
        incrementScreenHandlerSyncId();
        networkHandler.SendPacket(new OpenScreenS2CPacket(screenHandlerSyncId, 2, furnace.getName(), furnace.size()));
        currentScreenHandler = new FurnaceScreenHandler(inventory, furnace);
        currentScreenHandler.syncId = screenHandlerSyncId;
        currentScreenHandler.addListener(this);
    }


    public override void openDispenserScreen(BlockEntityDispenser dispenser)
    {
        incrementScreenHandlerSyncId();
        networkHandler.SendPacket(new OpenScreenS2CPacket(screenHandlerSyncId, 3, dispenser.getName(), dispenser.size()));
        currentScreenHandler = new DispenserScreenHandler(inventory, dispenser);
        currentScreenHandler.syncId = screenHandlerSyncId;
        currentScreenHandler.addListener(this);
    }


    public void onSlotUpdate(ScreenHandler handler, int slot, ItemStack stack)
    {
        if (handler.getSlot(slot) is not CraftingResultSlot)
        {
            if (!skipPacketSlotUpdates)
            {
                networkHandler.SendPacket(new ScreenHandlerSlotUpdateS2CPacket(handler.syncId, slot, stack));
            }
        }
    }

    public void onContentsUpdate(ScreenHandler screenHandler)
    {
        onContentsUpdate(screenHandler, screenHandler.getStacks());
    }


    public void onContentsUpdate(ScreenHandler handler, List stacks)
    {
        networkHandler.SendPacket(new InventoryS2CPacket(handler.syncId, stacks));
        networkHandler.SendPacket(new ScreenHandlerSlotUpdateS2CPacket(-1, -1, inventory.getCursorStack()));
    }

    public void onPropertyUpdate(ScreenHandler handler, int syncId, int trackedValue)
    {
        networkHandler.SendPacket(new ScreenHandlerPropertyUpdateS2CPacket(handler.syncId, syncId, trackedValue));
    }

    public override void onCursorStackChanged(ItemStack stack)
    {
    }

    public override void closeHandledScreen()
    {
        networkHandler.SendPacket(new CloseScreenS2CPacket(currentScreenHandler.syncId));
        onHandledScreenClosed();
    }

    public void updateCursorStack()
    {
        if (!skipPacketSlotUpdates)
        {
            networkHandler.SendPacket(new ScreenHandlerSlotUpdateS2CPacket(-1, -1, inventory.getCursorStack()));
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


    public override void increaseStat(StatBase stat, int amount)
    {
        if (stat != null)
        {
            if (!stat.LocalOnly)
            {
                while (amount > 100)
                {
                    networkHandler.SendPacket(new IncreaseStatS2CPacket(stat.Id, 100));
                    amount -= 100;
                }

                networkHandler.SendPacket(new IncreaseStatS2CPacket(stat.Id, amount));
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

    public void markHealthDirty()
    {
        lastHealthScore = -99999999;
    }

    public override void sendMessage(string message)
    {
        TranslationStorage ts = TranslationStorage.Instance;
        string translatedMessage = ts.TranslateKey(message);
        networkHandler.SendPacket(new ChatMessagePacket(translatedMessage));
    }

    public override void spawn()
    {
        //client only
        throw new NotImplementedException();
    }
}
