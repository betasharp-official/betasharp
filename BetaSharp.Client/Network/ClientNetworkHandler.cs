using System.Net;
using System.Net.Sockets;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Input;
using BetaSharp.Client.Worlds;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Screens;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;
using Socket = System.Net.Sockets.Socket;

namespace BetaSharp.Client.Network;

public class ClientNetworkHandler : NetHandler
{
    private readonly ILogger<ClientNetworkHandler> _logger = Log.Instance.For<ClientNetworkHandler>();

    private static readonly HttpClient _httpClient = new();

    private bool disconnected;
    private readonly Connection netManager;
    public string field_1209_a;
    private readonly BetaSharp _game;
    private ClientWorld worldClient;
    private bool terrainLoaded;
    public PersistentStateManager clientPersistentStateManager = new(null);
    readonly JavaRandom rand = new();

    private int ticks;
    private int lastKeepAliveTime;

    public ClientNetworkHandler(BetaSharp game, string address, int port)
    {
        this._game = game;

        var addresses = Dns.GetHostAddresses(address);
        var endPoint = new IPEndPoint(addresses.FirstOrDefault(a => a.AddressFamily is AddressFamily.InterNetwork) ?? addresses.First(), port);

        Socket socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        socket.Connect(endPoint);

        netManager = new Connection(socket, "Client", this);
    }

    public ClientNetworkHandler(BetaSharp game, Connection connection)
    {
        this._game = game;
        netManager = connection;
        netManager.setNetworkHandler(this);
    }

    public void tick()
    {
        if (!disconnected)
        {
            netManager.tick();

            if (ticks++ - lastKeepAliveTime > 200)
            {
                SendPacket(KeepAlivePacket.Get());
            }
        }

    }

    public void SendPacket(Packet packet)
    {
        if (!disconnected)
        {
            netManager.sendPacket(packet);
            lastKeepAliveTime = ticks;
        }
    }

    public override void onHello(LoginHelloPacket packet)
    {
        _game.playerController = new PlayerControllerMP(_game, this);
        _game.statFileWriter.ReadStat(Stats.Stats.JoinMultiplayerStat, 1);
        worldClient = new ClientWorld(this, packet.worldSeed, packet.dimensionId)
        {
            isRemote = true
        };
        _game.changeWorld(worldClient);
        _game.player.dimensionId = packet.dimensionId;
        _game.displayGuiScreen(new GuiDownloadTerrain(this));
        _game.player.id = packet.protocolVersion;
    }

    public override void onItemEntitySpawn(ItemEntitySpawnS2CPacket packet)
    {
        double x = packet.x / 32.0D;
        double y = packet.y / 32.0D;
        double z = packet.z / 32.0D;
        EntityItem entityItem = new(worldClient, x, y, z, new ItemStack(packet.itemRawId, packet.itemCount, packet.itemDamage))
        {
            velocityX = packet.velocityX / 128.0D,
            velocityY = packet.velocityY / 128.0D,
            velocityZ = packet.velocityZ / 128.0D,
            trackedPosX = packet.x,
            trackedPosY = packet.y,
            trackedPosZ = packet.z
        };
        worldClient.ForceEntity(packet.id, entityItem);
    }

    public override void onEntitySpawn(EntitySpawnS2CPacket packet)
    {
        double x = packet.x / 32.0D;
        double y = packet.y / 32.0D;
        double z = packet.z / 32.0D;
        object entity = null;
        if (packet.entityType == 10)
        {
            entity = new EntityMinecart(worldClient, x, y, z, 0);
        }

        if (packet.entityType == 11)
        {
            entity = new EntityMinecart(worldClient, x, y, z, 1);
        }

        if (packet.entityType == 12)
        {
            entity = new EntityMinecart(worldClient, x, y, z, 2);
        }

        if (packet.entityType == 90)
        {
            entity = new EntityFish(worldClient, x, y, z);
        }

        if (packet.entityType == 60)
        {
            entity = new EntityArrow(worldClient, x, y, z);
        }

        if (packet.entityType == 61)
        {
            entity = new EntitySnowball(worldClient, x, y, z);
        }

        if (packet.entityType == 63)
        {
            entity = new EntityFireball(worldClient, x, y, z, packet.velocityX / 8000.0D, packet.velocityY / 8000.0D, packet.velocityZ / 8000.0D);
            packet.entityData = 0;
        }

        if (packet.entityType == 62)
        {
            entity = new EntityEgg(worldClient, x, y, z);
        }

        if (packet.entityType == 1)
        {
            entity = new EntityBoat(worldClient, x, y, z);
        }

        if (packet.entityType == 50)
        {
            entity = new EntityTNTPrimed(worldClient, x, y, z);
        }

        if (packet.entityType == 70)
        {
            entity = new EntityFallingSand(worldClient, x, y, z, Block.Sand.id);
        }

        if (packet.entityType == 71)
        {
            entity = new EntityFallingSand(worldClient, x, y, z, Block.Gravel.id);
        }

        if (entity != null)
        {
            ((Entity)entity).trackedPosX = packet.x;
            ((Entity)entity).trackedPosY = packet.y;
            ((Entity)entity).trackedPosZ = packet.z;
            ((Entity)entity).yaw = 0.0F;
            ((Entity)entity).pitch = 0.0F;
            ((Entity)entity).id = packet.EntityId;
            worldClient.ForceEntity(packet.EntityId, (Entity)entity);
            if (packet.entityData > 0)
            {
                if (packet.entityType == 60)
                {
                    Entity owner = getEntityByID(packet.entityData);
                    if (owner is EntityLiving)
                    {
                        ((EntityArrow)entity).owner = (EntityLiving)owner;
                    }
                }

                ((Entity)entity).setVelocityClient(packet.velocityX / 8000.0D, packet.velocityY / 8000.0D, packet.velocityZ / 8000.0D);
            }
        }

    }

    public override void onLightningEntitySpawn(GlobalEntitySpawnS2CPacket packet)
    {
        double x = packet.x / 32.0D;
        double y = packet.y / 32.0D;
        double z = packet.z / 32.0D;
        EntityLightningBolt ent = null;
        if (packet.type == 1)
        {
            ent = new EntityLightningBolt(worldClient, x, y, z);
        }

        if (ent != null)
        {
            ent.trackedPosX = packet.x;
            ent.trackedPosY = packet.y;
            ent.trackedPosZ = packet.z;
            ent.yaw = 0.0F;
            ent.pitch = 0.0F;
            ent.id = packet.id;
            worldClient.spawnGlobalEntity(ent);
        }

    }

    public override void onPaintingEntitySpawn(PaintingEntitySpawnS2CPacket packet)
    {
        EntityPainting ent = new(worldClient, packet.xPosition, packet.yPosition, packet.zPosition, packet.direction, packet.title);
        worldClient.ForceEntity(packet.entityId, ent);
    }

    public override void onEntityVelocityUpdate(EntityVelocityUpdateS2CPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null)
        {
            ent.setVelocityClient(packet.motionX / 8000.0D, packet.motionY / 8000.0D, packet.motionZ / 8000.0D);
        }
    }

    public override void onEntityTrackerUpdate(EntityTrackerUpdateS2CPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null && packet.GetWatchedObjects() != null)
        {
            ent.getDataWatcher().UpdateWatchedObjectsFromList(packet.GetWatchedObjects());
        }

    }

    public override void onPlayerSpawn(PlayerSpawnS2CPacket packet)
    {
        double x = packet.xPosition / 32.0D;
        double y = packet.yPosition / 32.0D;
        double z = packet.zPosition / 32.0D;
        float rotation = packet.rotation * 360 / 256.0F;
        float pitch = packet.pitch * 360 / 256.0F;
        OtherPlayerEntity ent = new(_game.world, packet.name);
        ent.prevX = ent.lastTickX = ent.trackedPosX = packet.xPosition;
        ent.prevY = ent.lastTickY = ent.trackedPosY = packet.yPosition;
        ent.prevZ = ent.lastTickZ = ent.trackedPosZ = packet.zPosition;
        int currentItem = packet.currentItem;
        if (currentItem == 0)
        {
            ent.inventory.main[ent.inventory.selectedSlot] = null;
        }
        else
        {
            ent.inventory.main[ent.inventory.selectedSlot] = new ItemStack(currentItem, 1, 0);
        }

        ent.setPositionAndAngles(x, y, z, rotation, pitch);
        worldClient.ForceEntity(packet.entityId, ent);
    }

    public override void onEntityPosition(EntityPositionS2CPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null)
        {
            ent.trackedPosX = packet.x;
            ent.trackedPosY = packet.y;
            ent.trackedPosZ = packet.z;
            double posX = ent.trackedPosX / 32.0D;
            double posY = ent.trackedPosY / 32.0D + 1.0D / 64.0D;
            double posZ = ent.trackedPosZ / 32.0D;
            float yaw = packet.yaw * 360 / 256.0F;
            float pitch = packet.pitch * 360 / 256.0F;
            ent.setPositionAndAnglesAvoidEntities(posX, posY, posZ, yaw, pitch, 3);
        }
    }

    public override void onEntity(EntityS2CPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null)
        {
            ent.trackedPosX += packet.deltaX;
            ent.trackedPosY += packet.deltaY;
            ent.trackedPosZ += packet.deltaZ;
            double posX = ent.trackedPosX / 32.0D;
            double posY = ent.trackedPosY / 32.0D;
            double posZ = ent.trackedPosZ / 32.0D;
            float yaw = packet.rotate ? packet.yaw * 360 / 256.0F : ent.yaw;
            float pitch = packet.rotate ? packet.pitch * 360 / 256.0F : ent.pitch;
            ent.setPositionAndAnglesAvoidEntities(posX, posY, posZ, yaw, pitch, 3);
        }
    }

    public override void onEntityDestroy(EntityDestroyS2CPacket packet)
    {
        worldClient.RemoveEntityFromWorld(packet.EntityId);
    }

    public override void onPlayerMove(PlayerMovePacket packet)
    {
        ClientPlayerEntity ent = _game.player;
        double x = ent.x;
        double y = ent.y;
        double z = ent.z;
        float yaw = ent.yaw;
        float pitch = ent.pitch;
        if (packet.changePosition)
        {
            x = packet.x;
            y = packet.y;
            z = packet.z;
        }

        if (packet.changeLook)
        {
            yaw = packet.yaw;
            pitch = packet.pitch;
        }

        ent.cameraOffset = 0.0F;
        ent.velocityX = ent.velocityY = ent.velocityZ = 0.0D;
        ent.setPositionAndAngles(x, y, z, yaw, pitch);
        packet.x = ent.x;
        packet.y = ent.boundingBox.MinY;
        packet.z = ent.z;
        packet.eyeHeight = ent.y;
        SendPacket(packet);
        if (!terrainLoaded)
        {
            _game.player.prevX = _game.player.x;
            _game.player.prevY = _game.player.y;
            _game.player.prevZ = _game.player.z;
            terrainLoaded = true;
            _game.displayGuiScreen(null);
        }

    }

    public override void onChunkStatusUpdate(ChunkStatusUpdateS2CPacket packet)
    {
        worldClient.UpdateChunk(packet.x, packet.z, packet.load);
    }

    public override void onChunkDeltaUpdate(ChunkDeltaUpdateS2CPacket packet)
    {
        Chunk chunk = worldClient.GetChunk(packet.x, packet.z);
        int x = packet.x * 16;
        int y = packet.z * 16;

        for (int i = 0; i < packet._size; ++i)
        {
            short positions = packet.positions[i];
            int blockRawId = packet.blockRawIds[i] & 255;
            byte metadata = packet.blockMetadata[i];
            int blockX = positions >> 12 & 15;
            int blockZ = positions >> 8 & 15;
            int blockY = positions & 255;
            chunk.SetBlock(blockX, blockY, blockZ, blockRawId, metadata);
            worldClient.ClearBlockResets(blockX + x, blockY, blockZ + y, blockX + x, blockY, blockZ + y);
            worldClient.setBlocksDirty(blockX + x, blockY, blockZ + y, blockX + x, blockY, blockZ + y);
        }

    }

    public override void handleChunkData(ChunkDataS2CPacket packet)
    {
        worldClient.ClearBlockResets(packet.x, packet.y, packet.z, packet.x + packet.sizeX - 1, packet.y + packet.sizeY - 1, packet.z + packet.sizeZ - 1);
        worldClient.handleChunkDataUpdate(packet.x, packet.y, packet.z, packet.sizeX, packet.sizeY, packet.sizeZ, packet.chunkData);
    }

    public override void onBlockUpdate(BlockUpdateS2CPacket packet)
    {
        worldClient.SetBlockWithMetaFromPacket(packet.x, packet.y, packet.z, packet.blockRawId, packet.blockMetadata);
    }

    public override void onDisconnect(DisconnectPacket packet)
    {
        netManager.disconnect("disconnect.kicked");
        disconnected = true;
        _game.changeWorld(null);
        _game.displayGuiScreen(new GuiConnectFailed("disconnect.disconnected", "disconnect.genericReason", packet.reason));
    }

    public override void onDisconnected(string reason, object[]? args)
    {
        if (!disconnected)
        {
            disconnected = true;
            _game.changeWorld(null);
            _game.displayGuiScreen(new GuiConnectFailed("disconnect.lost", reason, args));
        }
    }

    public void sendPacketAndDisconnect(Packet packet)
    {
        if (!disconnected)
        {
            SendPacket(packet);
            netManager.disconnect();
        }
    }

    public void addToSendQueue(Packet packet)
    {
        if (!disconnected)
        {
            SendPacket(packet);
        }
    }

    public override void onItemPickupAnimation(ItemPickupAnimationS2CPacket packet)
    {
        Entity? ent = getEntityByID(packet.entityId);
        Entity collector = getEntityByID(packet.collectorEntityId) as EntityLiving ?? _game.player;

        if (ent != null && collector != null)
        {
            worldClient.playSound(ent, "random.pop", 0.2F, ((rand.NextFloat() - rand.NextFloat()) * 0.7F + 1.0F) * 2.0F);
            _game.particleManager.addEffect(new EntityPickupFX(_game.world, ent, collector, -0.5F));
            worldClient.RemoveEntityFromWorld(packet.entityId);
        }

    }

    public override void onChatMessage(ChatMessagePacket packet)
    {
        _game.ingameGUI.addChatMessage(packet.chatMessage);
    }

    public override void onEntityAnimation(EntityAnimationPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null)
        {
            if (packet.animationId == 1)
            {
                if (ent is EntityPlayer player)
                    player.swingHand();
            }
            else if (packet.animationId == 2)
            {
                ent.animateHurt();
            }
            else if (packet.animationId == 3)
            {
                if (ent is EntityPlayer player)
                    player.wakeUp(false, false, false);
            }
            else if (packet.animationId == 4)
            {
                if (ent is EntityPlayer player)
                    player.spawn();
            }

        }
    }

    public override void onPlayerSleepUpdate(PlayerSleepUpdateS2CPacket packet)
    {
        Entity? ent = getEntityByID(packet.id);
        if (ent is EntityPlayer player)
        {
            if (packet.status == 0)
            {
                player.trySleep(packet.x, packet.y, packet.z);
            }

        }
    }

    public override void onHandshake(HandshakePacket packet)
    {
        if (packet.username.Equals("-"))
        {
            addToSendQueue(new LoginHelloPacket(_game.session.username, 14, LoginHelloPacket.BETASHARP_CLIENT_SIGNATURE, 0));
        }
        else
        {
            try
            {
                //TODO: AUTH
                string authUrl = "http://www.minecraft.net/game/joinserver.jsp?user=" + _game.session.username + "&sessionId=" + _game.session.sessionId + "&serverId=" + packet.username;

                string? response = _httpClient.GetStringAsync(authUrl).GetAwaiter().GetResult();
                response = response?.Trim();

                if (string.IsNullOrEmpty(response) || response.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    addToSendQueue(new LoginHelloPacket(_game.session.username, 14, LoginHelloPacket.BETASHARP_CLIENT_SIGNATURE, 0));
                }
                else
                {
                    netManager.disconnect("disconnect.loginFailedInfo", response);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                netManager.disconnect("disconnect.genericReason", "Internal client error: " + e.Message);
            }
        }
    }

    public void disconnect()
    {
        disconnected = true;
        netManager.disconnect("disconnect.closed");
    }

    public override void onLivingEntitySpawn(LivingEntitySpawnS2CPacket packet)
    {
        double x = packet.xPosition / 32.0D;
        double y = packet.yPosition / 32.0D;
        double z = packet.zPosition / 32.0D;
        float yaw = packet.yaw * 360 / 256.0F;
        float pitch = packet.pitch * 360 / 256.0F;
        EntityLiving ent = (EntityLiving)EntityRegistry.Create(packet.type, _game.world);
        ent.trackedPosX = packet.xPosition;
        ent.trackedPosY = packet.yPosition;
        ent.trackedPosZ = packet.zPosition;
        ent.id = packet.entityId;
        ent.setPositionAndAngles(x, y, z, yaw, pitch);
        ent.interpolateOnly = true;
        worldClient.ForceEntity(packet.entityId, ent);
        List<WatchableObject> metaData = packet.GetMetadata();
        if (metaData != null)
        {
            ent.getDataWatcher().UpdateWatchedObjectsFromList(metaData);
        }

    }

    public override void onWorldTimeUpdate(WorldTimeUpdateS2CPacket packet)
    {
        _game.world.setTime(packet.time);
    }

    public override void onPlayerSpawnPosition(PlayerSpawnPositionS2CPacket packet)
    {
        _game.player.setSpawnPos(new Vec3i(packet.x, packet.y, packet.z));
        _game.world.getProperties().SetSpawn(packet.x, packet.y, packet.z);
    }

    public override void onEntityVehicleSet(EntityVehicleSetS2CPacket packet)
    {
        object rider = getEntityByID(packet.EntityId);
        Entity ent = getEntityByID(packet.VehicleEntityId);
        if (packet.EntityId == _game.player.id)
        {
            rider = _game.player;
        }

        if (rider is Entity riderEntity)
        {
            riderEntity.setVehicle(ent);
        }
    }

    public override void onEntityStatus(EntityStatusS2CPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null)
        {
            ent.processServerEntityStatus(packet.EntityStatus);
        }

    }

    private Entity? getEntityByID(int entityId)
    {
        if (_game == null || _game.player == null || worldClient == null)
        {
            return null;
        }

        return entityId == _game.player.id ? _game.player : worldClient.GetEntity(entityId);
    }

    public override void onHealthUpdate(HealthUpdateS2CPacket packet)
    {
        _game.player.setHealth(packet.healthMP);
    }

    public override void onPlayerRespawn(PlayerRespawnPacket packet)
    {
        if (packet.dimensionId != _game.player.dimensionId)
        {
            terrainLoaded = false;
            worldClient = new ClientWorld(this, worldClient.getProperties().RandomSeed, packet.dimensionId)
            {
                isRemote = true
            };
            _game.changeWorld(worldClient);
            _game.player.dimensionId = packet.dimensionId;
            _game.displayGuiScreen(new GuiDownloadTerrain(this));
        }

        _game.respawn(true, packet.dimensionId);
    }

    public override void onExplosion(ExplosionS2CPacket packet)
    {
        Explosion explosion = new(_game.world, null, packet.explosionX, packet.explosionY, packet.explosionZ, packet.explosionSize)
        {
            destroyedBlockPositions = packet.destroyedBlockPositions
        };
        explosion.doExplosionB(true);
    }

    public override void onOpenScreen(OpenScreenS2CPacket packet)
    {
        if (packet.screenHandlerId == 0)
        {
            InventoryBasic inventory = new(packet.name, packet.slotsCount);
            _game.player.openChestScreen(inventory);
            _game.player.currentScreenHandler.SyncId = packet.syncId;
        }
        else if (packet.screenHandlerId == 2)
        {
            BlockEntityFurnace furnace = new();
            _game.player.openFurnaceScreen(furnace);
            _game.player.currentScreenHandler.SyncId = packet.syncId;
        }
        else if (packet.screenHandlerId == 3)
        {
            BlockEntityDispenser dispenser = new();
            _game.player.openDispenserScreen(dispenser);
            _game.player.currentScreenHandler.SyncId = packet.syncId;
        }
        else if (packet.screenHandlerId == 1)
        {
            ClientPlayerEntity player = _game.player;
            _game.player.openCraftingScreen(MathHelper.Floor(player.x), MathHelper.Floor(player.y), MathHelper.Floor(player.z));
            _game.player.currentScreenHandler.SyncId = packet.syncId;
        }

    }

    public override void onScreenHandlerSlotUpdate(ScreenHandlerSlotUpdateS2CPacket packet)
    {
        if (packet.syncId == -1)
        {
            _game.player.inventory.setItemStack(packet.stack);
        }
        else if (packet.syncId == 0 && packet.slot >= 36 && packet.slot < 45)
        {
            ItemStack itemStack = _game.player.playerScreenHandler.GetSlot(packet.slot).getStack();
            if (packet.stack != null && (itemStack == null || itemStack.count < packet.stack.count))
            {
                packet.stack.bobbingAnimationTime = 5;
            }

            _game.player.playerScreenHandler.setStackInSlot(packet.slot, packet.stack);
        }
        else if (packet.syncId == _game.player.currentScreenHandler.SyncId)
        {
            _game.player.currentScreenHandler.setStackInSlot(packet.slot, packet.stack);
        }

    }

    public override void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket packet)
    {
        ScreenHandler screenHandler = null;
        if (packet.syncId == 0)
        {
            screenHandler = _game.player.playerScreenHandler;
        }
        else if (packet.syncId == _game.player.currentScreenHandler.SyncId)
        {
            screenHandler = _game.player.currentScreenHandler;
        }

        if (screenHandler != null)
        {
            if (packet.accepted)
            {
                screenHandler.onAcknowledgementAccepted(packet.actionType);
            }
            else
            {
                screenHandler.onAcknowledgementDenied(packet.actionType);
                addToSendQueue(ScreenHandlerAcknowledgementPacket.Get(packet.syncId, packet.actionType, true));
            }
        }

    }

    public override void onInventory(InventoryS2CPacket packet)
    {
        if (packet.syncId == 0)
        {
            _game.player.playerScreenHandler.updateSlotStacks(packet.contents);
        }
        else if (packet.syncId == _game.player.currentScreenHandler.SyncId)
        {
            _game.player.currentScreenHandler.updateSlotStacks(packet.contents);
        }

    }

    public override void handleUpdateSign(UpdateSignPacket packet)
    {
        if (_game.world.isPosLoaded(packet.x, packet.y, packet.z))
        {
            BlockEntity blockEnt = _game.world.getBlockEntity(packet.x, packet.y, packet.z);
            if (blockEnt is BlockEntitySign)
            {
                BlockEntitySign signEntity = (BlockEntitySign)blockEnt;

                for (int i = 0; i < 4; ++i)
                {
                    signEntity.Texts[i] = packet.text[i];
                }

                signEntity.markDirty();
            }
        }

    }

    public override void onScreenHandlerPropertyUpdate(ScreenHandlerPropertyUpdateS2CPacket packet)
    {
        handle(packet);
        if (_game.player.currentScreenHandler != null && _game.player.currentScreenHandler.SyncId == packet.syncId)
        {
            _game.player.currentScreenHandler.setProperty(packet.propertyId, packet.value);
        }

    }

    public override void onEntityEquipmentUpdate(EntityEquipmentUpdateS2CPacket packet)
    {
        Entity ent = getEntityByID(packet.EntityId);
        if (ent != null)
        {
            ent.setEquipmentStack(packet.slot, packet.itemRawId, packet.itemDamage);
        }

    }

    public override void onCloseScreen(CloseScreenS2CPacket packet)
    {
        _game.player.closeHandledScreen();
    }

    public override void onPlayNoteSound(PlayNoteSoundS2CPacket packet)
    {
        _game.world.playNoteBlockActionAt(packet.xLocation, packet.yLocation, packet.zLocation, packet.instrumentType, packet.pitch);
    }

    public override void onGameStateChange(GameStateChangeS2CPacket packet)
    {
        int reason = packet.reason;
        if (reason >= 0 && reason < GameStateChangeS2CPacket.REASONS.Length && GameStateChangeS2CPacket.REASONS[reason] != null)
        {
            _game.player.sendMessage(GameStateChangeS2CPacket.REASONS[reason]);
        }

        if (reason == 1)
        {
            worldClient.getProperties().IsRaining = true;
            worldClient.setRainGradient(1.0F);
        }
        else if (reason == 2)
        {
            worldClient.getProperties().IsRaining = false;
            worldClient.setRainGradient(0.0F);
        }

    }

    public override void onMapUpdate(MapUpdateS2CPacket packet)
    {
        if (packet.itemRawId == Item.Map.id)
        {
            ItemMap.getMapState(packet.id, _game.world).updateData(packet.updateData);
        }
        else
        {
            _logger.LogInformation($"Unknown itemid: {packet.id}");
        }

    }

    public override void onWorldEvent(WorldEventS2CPacket packet)
    {
        _game.world.worldEvent(packet.eventId, packet.x, packet.y, packet.z, packet.data);
    }

    public override void onIncreaseStat(IncreaseStatS2CPacket packet)
    {
        try
        {
            StatBase stat = Stats.Stats.GetStatById(packet.statId);
            ((EntityClientPlayerMP)_game.player).func_27027_b(stat, packet.amount);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Unknown stat id in IncreaseStatS2CPacket: {StatId}", packet.statId);
        }
    }

    public override void onPlayerConnectionUpdate(PlayerConnectionUpdateS2CPacket packet)
    {
        if (packet.type == PlayerConnectionUpdateS2CPacket.ConnectionUpdateType.Leave)
        {
            Entity ent = worldClient.GetEntity(packet.entityId);
            EntityRenderDispatcher.instance.skinManager?.Release(packet.name);
        }
    }

    public override bool isServerSide()
    {
        return false;
    }
}
