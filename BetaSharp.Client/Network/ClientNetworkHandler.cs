using System.Net;
using System.Net.Sockets;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Worlds;
using BetaSharp.Diagnostics;
using BetaSharp.Entities;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Network;
using BetaSharp.Network.Packets;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Registries;
using BetaSharp.Screens;
using BetaSharp.Stats;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Mechanics;
using BetaSharp.Worlds.Storage;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Network;

public class ClientNetworkHandler : NetHandler
{
    private readonly ILogger<ClientNetworkHandler> _logger = Log.Instance.For<ClientNetworkHandler>();

    public bool Disconnected { get; private set; }
    private readonly Connection _netManager;
    public string StatusMessage;
    private readonly ClientNetworkContext _context;
    private ClientWorld _worldClient;
    private bool _terrainLoaded;
    public PersistentStateManager ClientPersistentStateManager { get; } = new(null);
    private readonly JavaRandom _rand = new();

    private int _ticks;
    private int _lastKeepAliveTime;

    private readonly ClientRegistryAccess _clientRegistries = new();

    public ClientNetworkHandler(ClientNetworkContext context, string address, int port)
    {
        _context = context;

        IPAddress[] addresses = Dns.GetHostAddresses(address);
        var endPoint = new IPEndPoint(addresses.FirstOrDefault(a => a.AddressFamily is AddressFamily.InterNetwork) ?? addresses.First(), port);

        Socket socket = new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        socket.Connect(endPoint);

        _netManager = new Connection(socket, "Client", this);
    }

    public ClientNetworkHandler(ClientNetworkContext context, Connection connection)
    {
        _context = context;
        _netManager = connection;
    }

    public void Tick()
    {
        if (!Disconnected)
        {
            _netManager.tick();

            MetricRegistry.Set(ClientMetrics.UploadBytes, _netManager.BytesWritten);
            MetricRegistry.Set(ClientMetrics.DownloadBytes, _netManager.BytesRead);
            MetricRegistry.Set(ClientMetrics.UploadPackets, _netManager.PacketsWritten);
            MetricRegistry.Set(ClientMetrics.DownloadPackets, _netManager.PacketsRead);
            MetricRegistry.Set(ClientMetrics.IsInternal, _netManager is InternalConnection);
            MetricRegistry.Set(ClientMetrics.ServerAddress, _netManager.getAddress()?.ToString() ?? "Unknown");

            if (_ticks++ - _lastKeepAliveTime > 200)
            {
                SendPacket(KeepAlivePacket.Get());
            }
        }
    }

    public void SendPacket(Packet packet)
    {
        if (!Disconnected)
        {
            _netManager.sendPacket(packet);
            _lastKeepAliveTime = _ticks;
        }
        else
        {
            packet.Return();
        }
    }

    public override void onHello(LoginHelloPacket packet)
    {
        _logger.LogInformation($"[Client] Received onHello from server (id: {packet.protocolVersion})");
        _context.PlayerHost.SetPlayerController(_context.Factory.CreatePlayerController(this));
        _context.StatFileWriter.ReadStat(Stats.Stats.JoinMultiplayerStat, 1);
        _worldClient = new ClientWorld(this, packet.worldSeed, packet.dimensionId)
        {
            IsRemote = true
        };
        _context.WorldHost.ChangeWorld(_worldClient);
        _context.PlayerHost.Player.dimensionId = packet.dimensionId;
        _context.Navigator.Navigate(_context.Factory.CreateTerrainScreen(this));
        _context.PlayerHost.Player.id = packet.protocolVersion;
    }

    public override void onItemEntitySpawn(ItemEntitySpawnS2CPacket packet)
    {
        double x = packet.x / 32.0D;
        double y = packet.y / 32.0D;
        double z = packet.z / 32.0D;
        EntityItem entityItem = new(_worldClient, x, y, z, new ItemStack(packet.itemRawId, packet.itemCount, packet.itemDamage))
        {
            velocityX = packet.velocityX / 128.0D,
            velocityY = packet.velocityY / 128.0D,
            velocityZ = packet.velocityZ / 128.0D,
            trackedPosX = packet.x,
            trackedPosY = packet.y,
            trackedPosZ = packet.z
        };
        _worldClient.ForceEntity(packet.id, entityItem);
    }

    public override void onEntitySpawn(EntitySpawnS2CPacket packet)
    {
        double x = packet.x / 32.0D;
        double y = packet.y / 32.0D;
        double z = packet.z / 32.0D;
        object? entity = null;
        if (packet.entityType == 10)
        {
            entity = new EntityMinecart(_worldClient, x, y, z, 0);
        }

        if (packet.entityType == 11)
        {
            entity = new EntityMinecart(_worldClient, x, y, z, 1);
        }

        if (packet.entityType == 12)
        {
            entity = new EntityMinecart(_worldClient, x, y, z, 2);
        }

        if (packet.entityType == 90)
        {
            entity = new EntityFish(_worldClient, x, y, z);
        }

        if (packet.entityType == 60)
        {
            entity = new EntityArrow(_worldClient, x, y, z);
        }

        if (packet.entityType == 61)
        {
            entity = new EntitySnowball(_worldClient, x, y, z);
        }

        if (packet.entityType == 63)
        {
            entity = new EntityFireball(_worldClient, x, y, z, packet.velocityX / 8000.0D, packet.velocityY / 8000.0D, packet.velocityZ / 8000.0D);
            packet.entityData = 0;
        }

        if (packet.entityType == 62)
        {
            entity = new EntityEgg(_worldClient, x, y, z);
        }

        if (packet.entityType == 1)
        {
            entity = new EntityBoat(_worldClient, x, y, z);
        }

        if (packet.entityType == 50)
        {
            entity = new EntityTNTPrimed(_worldClient, x, y, z);
        }

        if (packet.entityType == 70)
        {
            entity = new EntityFallingSand(_worldClient, x, y, z, Block.Sand.id);
        }

        if (packet.entityType == 71)
        {
            entity = new EntityFallingSand(_worldClient, x, y, z, Block.Gravel.id);
        }

        if (entity != null)
        {
            ((Entity)entity).trackedPosX = packet.x;
            ((Entity)entity).trackedPosY = packet.y;
            ((Entity)entity).trackedPosZ = packet.z;
            ((Entity)entity).yaw = 0.0F;
            ((Entity)entity).pitch = 0.0F;
            ((Entity)entity).id = packet.EntityId;
            _worldClient.ForceEntity(packet.EntityId, (Entity)entity);
            if (packet.entityData > 0)
            {
                if (packet.entityType == 60)
                {
                    Entity? owner = GetEntityById(packet.entityData);
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
        EntityLightningBolt? ent = null;
        if (packet.type == 1)
        {
            ent = new EntityLightningBolt(_worldClient, x, y, z);
        }

        if (ent != null)
        {
            ent.trackedPosX = packet.x;
            ent.trackedPosY = packet.y;
            ent.trackedPosZ = packet.z;
            ent.yaw = 0.0F;
            ent.pitch = 0.0F;
            ent.id = packet.id;
            _worldClient.Entities.SpawnGlobalEntity(ent);
        }

    }

    public override void onPaintingEntitySpawn(PaintingEntitySpawnS2CPacket packet)
    {
        EntityPainting ent = new(_worldClient, packet.xPosition, packet.yPosition, packet.zPosition, packet.direction, packet.title);
        _worldClient.ForceEntity(packet.entityId, ent);
    }

    public override void onEntityVelocityUpdate(EntityVelocityUpdateS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        ent?.setVelocityClient(packet.motionX / 8000.0D, packet.motionY / 8000.0D, packet.motionZ / 8000.0D);
    }

    public override void onEntityTrackerUpdate(EntityTrackerUpdateS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        if (ent == null || packet.Data == null || packet.Data.Length == 0)
        {
            return;
        }

        ent.DataSynchronizer.ApplyChanges(new MemoryStream(packet.Data));
    }

    public override void onPlayerSpawn(PlayerSpawnS2CPacket packet)
    {
        double x = packet.xPosition / 32.0D;
        double y = packet.yPosition / 32.0D;
        double z = packet.zPosition / 32.0D;
        float rotation = packet.rotation * 360 / 256.0F;
        float pitch = packet.pitch * 360 / 256.0F;
        OtherPlayerEntity ent = new(_context.WorldHost.World, packet.name);
        ent.prevX = ent.lastTickX = ent.trackedPosX = packet.xPosition;
        ent.prevY = ent.lastTickY = ent.trackedPosY = packet.yPosition;
        ent.prevZ = ent.lastTickZ = ent.trackedPosZ = packet.zPosition;
        int currentItem = packet.currentItem;
        if (currentItem == 0)
        {
            ent.inventory.Main[ent.inventory.SelectedSlot] = null;
        }
        else
        {
            ent.inventory.Main[ent.inventory.SelectedSlot] = new ItemStack(currentItem, 1, 0);
        }

        ent.setPositionAndAngles(x, y, z, rotation, pitch);
        _worldClient.ForceEntity(packet.entityId, ent);
    }

    public override void onEntityPosition(EntityPositionS2CPacket packet)
    {
        Entity ent = GetEntityById(packet.EntityId);
        if (ent != null)
        {
            ent.trackedPosX = packet.x;
            ent.trackedPosY = packet.y;
            ent.trackedPosZ = packet.z;
            double posX = ent.trackedPosX / 32.0D;
            double posY = ent.trackedPosY / 32.0D;
            double posZ = ent.trackedPosZ / 32.0D;
            float yaw = packet.yaw * 360 / 256.0F;
            float pitch = packet.pitch * 360 / 256.0F;
            ent.setPositionAndAnglesAvoidEntities(posX, posY, posZ, yaw, pitch, 5);
        }
    }

    public override void onEntity(EntityS2CPacket packet)
    {
        Entity ent = GetEntityById(packet.EntityId);
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
            ent.setPositionAndAnglesAvoidEntities(posX, posY, posZ, yaw, pitch, 5);
        }
    }

    public override void onEntityDestroy(EntityDestroyS2CPacket packet)
    {
        _worldClient.RemoveEntityFromWorld(packet.EntityId);
    }

    public override void onPlayerMove(PlayerMovePacket packet)
    {
        ClientPlayerEntity ent = _context.PlayerHost.Player;
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
        if (!_terrainLoaded)
        {
            ClientPlayerEntity player = _context.PlayerHost.Player;
            player.prevX = player.x;
            player.prevY = player.y;
            player.prevZ = player.z;
            _terrainLoaded = true;
            _context.Navigator.Navigate(null);
        }

    }

    public override void onChunkStatusUpdate(ChunkStatusUpdateS2CPacket packet)
    {
        _worldClient.UpdateChunk(packet.x, packet.z, packet.load);
    }

    public override void onChunkDeltaUpdate(ChunkDeltaUpdateS2CPacket packet)
    {
        Chunk chunk = _worldClient.BlockHost.GetChunk(packet.x, packet.z);
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
            _worldClient.ClearBlockResets(blockX + x, blockY, blockZ + y, blockX + x, blockY, blockZ + y);
            _worldClient.setBlocksDirty(blockX + x, blockY, blockZ + y, blockX + x, blockY, blockZ + y);
        }

    }

    public override void handleChunkData(ChunkDataS2CPacket packet)
    {
        _worldClient.ClearBlockResets(packet.x, packet.y, packet.z, packet.x + packet.sizeX - 1, packet.y + packet.sizeY - 1, packet.z + packet.sizeZ - 1);
        _worldClient.HandleChunkDataUpdate(packet.x, packet.y, packet.z, packet.sizeX, packet.sizeY, packet.sizeZ, packet.chunkData);
    }

    public override void onBlockUpdate(BlockUpdateS2CPacket packet)
    {
        _worldClient.SetBlockWithMetaFromPacket(packet.x, packet.y, packet.z, packet.blockRawId, packet.blockMetadata);
    }

    public override void onDisconnect(DisconnectPacket packet)
    {
        _netManager.disconnect("disconnect.kicked");
        Disconnected = true;
        _context.WorldHost.ChangeWorld(null);
        _context.Navigator.Navigate(_context.Factory.CreateFailedScreen("disconnect.disconnected", "disconnect.genericReason", [packet.reason]));
    }

    public override void onDisconnected(string reason, object[]? args)
    {
        if (!Disconnected)
        {
            Disconnected = true;
            _context.WorldHost.ChangeWorld(null);
            _context.Navigator.Navigate(_context.Factory.CreateFailedScreen("disconnect.lost", reason, args));
        }
    }

    public void SendPacketAndDisconnect(Packet packet)
    {
        if (!Disconnected)
        {
            SendPacket(packet);
            _netManager.disconnect();
        }
    }

    public void AddToSendQueue(Packet packet)
    {
        SendPacket(packet);
    }

    public override void onItemPickupAnimation(ItemPickupAnimationS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.entityId);
        Entity collector = GetEntityById(packet.collectorEntityId) as EntityLiving ?? _context.PlayerHost.Player;

        if (ent != null && collector != null)
        {
            _worldClient.Broadcaster.PlaySoundAtEntity(ent, "random.pop", 0.2F, ((_rand.NextFloat() - _rand.NextFloat()) * 0.7F + 1.0F) * 2.0F);
            _context.ParticleManager.AddPickupParticle(ent, collector, -0.5F, _context.EntityRenderDispatcher);
            _worldClient.RemoveEntityFromWorld(packet.entityId);
        }

    }

    public override void onChatMessage(ChatMessagePacket packet)
    {
        _context.AddChatMessage(packet.chatMessage);
    }

    public override void onEntityAnimation(EntityAnimationPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
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
        Entity? ent = GetEntityById(packet.id);
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
        AddToSendQueue(new LoginHelloPacket(_context.Session.username, 14, LoginHelloPacket.BETASHARP_CLIENT_SIGNATURE, 0));
    }

    public void Disconnect()
    {
        Disconnected = true;
        _netManager.disconnect("disconnect.closed");
    }

    public override void onLivingEntitySpawn(LivingEntitySpawnS2CPacket packet)
    {
        double x = packet.xPosition / 32.0D;
        double y = packet.yPosition / 32.0D;
        double z = packet.zPosition / 32.0D;
        float yaw = packet.yaw * 360 / 256.0F;
        float pitch = packet.pitch * 360 / 256.0F;
        EntityLiving ent = (EntityLiving)EntityRegistry.Create(packet.type, _context.WorldHost.World);
        ent.trackedPosX = packet.xPosition;
        ent.trackedPosY = packet.yPosition;
        ent.trackedPosZ = packet.zPosition;
        ent.id = packet.entityId;
        ent.setPositionAndAngles(x, y, z, yaw, pitch);
        ent.lastTickX = ent.x;
        ent.lastTickY = ent.y;
        ent.lastTickZ = ent.z;
        ent.interpolateOnly = true;
        _worldClient.ForceEntity(packet.entityId, ent);
        ent.DataSynchronizer.ApplyChanges(new MemoryStream(packet.Data));
    }

    public override void onWorldTimeUpdate(WorldTimeUpdateS2CPacket packet)
    {
        _context.WorldHost.World?.SetTime(packet.time);
    }

    public override void onPlayerSpawnPosition(PlayerSpawnPositionS2CPacket packet)
    {
        _context.PlayerHost.Player.setSpawnPos(new Vec3i(packet.x, packet.y, packet.z));
        _context.WorldHost.World?.Properties.SetSpawn(packet.x, packet.y, packet.z);
    }

    public override void onEntityVehicleSet(EntityVehicleSetS2CPacket packet)
    {
        object? rider = GetEntityById(packet.EntityId);
        Entity? ent = GetEntityById(packet.VehicleEntityId);
        if (packet.EntityId == _context.PlayerHost.Player.id)
        {
            rider = _context.PlayerHost.Player;
        }

        if (rider is Entity riderEntity)
        {
            riderEntity.setVehicle(ent);
        }
    }

    public override void onEntityStatus(EntityStatusS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        ent?.processServerEntityStatus(packet.EntityStatus);

    }

    private Entity? GetEntityById(int entityId)
    {
        if (_context.PlayerHost.Player == null || _worldClient == null)
        {
            return null;
        }

        return entityId == _context.PlayerHost.Player.id ? _context.PlayerHost.Player : _worldClient.GetEntity(entityId);
    }

    public override void onHealthUpdate(HealthUpdateS2CPacket packet)
    {
        _context.PlayerHost.Player.setHealth(packet.healthMP);
    }

    public override void onPlayerRespawn(PlayerRespawnPacket packet)
    {
        if (packet.dimensionId != _context.PlayerHost.Player.dimensionId)
        {
            _terrainLoaded = false;
            _worldClient = new ClientWorld(this, _worldClient.Properties.RandomSeed, packet.dimensionId)
            {
                IsRemote = true
            };
            _context.WorldHost.ChangeWorld(_worldClient);
            _context.PlayerHost.Player.dimensionId = packet.dimensionId;
            _context.Navigator.Navigate(_context.Factory.CreateTerrainScreen(this));
        }

        _context.PlayerHost.Respawn(true, packet.dimensionId);
    }

    public override void onExplosion(ExplosionS2CPacket packet)
    {
        Explosion explosion = new(_context.WorldHost.World, null, packet.explosionX, packet.explosionY, packet.explosionZ, packet.explosionSize)
        {
            destroyedBlockPositions = packet.destroyedBlockPositions
        };
        explosion.doExplosionB(true);
    }

    public override void onOpenScreen(OpenScreenS2CPacket packet)
    {
        ClientPlayerEntity player = _context.PlayerHost.Player;
        if (!player.GameMode.CanInteract) return;

        if (packet.screenHandlerId == 0)
        {
            InventoryBasic inventory = new(packet.name, packet.slotsCount);
            player.openChestScreen(inventory);
            player.currentScreenHandler.SyncId = packet.syncId;
        }
        else if (packet.screenHandlerId == 2)
        {
            BlockEntityFurnace furnace = new();
            player.openFurnaceScreen(furnace);
            player.currentScreenHandler.SyncId = packet.syncId;
        }
        else if (packet.screenHandlerId == 3)
        {
            BlockEntityDispenser dispenser = new();
            player.openDispenserScreen(dispenser);
            player.currentScreenHandler.SyncId = packet.syncId;
        }
        else if (packet.screenHandlerId == 1)
        {
            player.openCraftingScreen(MathHelper.Floor(player.x), MathHelper.Floor(player.y), MathHelper.Floor(player.z));
            player.currentScreenHandler.SyncId = packet.syncId;
        }

    }

    public override void onScreenHandlerSlotUpdate(ScreenHandlerSlotUpdateS2CPacket packet)
    {
        ClientPlayerEntity? player = _context.PlayerHost.Player;
        if (packet.syncId == -1)
        {
            player.inventory.SetCursorStack(packet.stack);
        }
        else if (packet.syncId == 0 && packet.slot >= 36 && packet.slot < 45)
        {
            ItemStack? itemStack = player.playerScreenHandler.GetSlot(packet.slot).getStack();
            if (packet.stack != null && (itemStack == null || itemStack.Count < packet.stack.Count))
            {
                packet.stack.AnimationTime = 5;
            }

            player.playerScreenHandler.setStackInSlot(packet.slot, packet.stack);
        }
        else if (packet.syncId == player.currentScreenHandler.SyncId)
        {
            player.currentScreenHandler.setStackInSlot(packet.slot, packet.stack);
        }

    }

    public override void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket packet)
    {
        ClientPlayerEntity player = _context.PlayerHost.Player;
        ScreenHandler? screenHandler = null;
        if (packet.syncId == 0)
        {
            screenHandler = player.playerScreenHandler;
        }
        else if (packet.syncId == player.currentScreenHandler.SyncId)
        {
            screenHandler = player.currentScreenHandler;
        }

        if (screenHandler != null)
        {
            if (packet.accepted)
            {
                ScreenHandler.onAcknowledgementAccepted(packet.actionType);
            }
            else
            {
                ScreenHandler.onAcknowledgementDenied(packet.actionType);
                AddToSendQueue(ScreenHandlerAcknowledgementPacket.Get(packet.syncId, packet.actionType, true));
            }
        }

    }

    public override void onInventory(InventoryS2CPacket packet)
    {
        ClientPlayerEntity? player = _context.PlayerHost.Player;
        if (packet.syncId == 0)
        {
            player.playerScreenHandler.updateSlotStacks(packet.contents);
        }
        else if (packet.syncId == player.currentScreenHandler.SyncId)
        {
            player.currentScreenHandler.updateSlotStacks(packet.contents);
        }

    }

    public override void handleUpdateSign(UpdateSignPacket packet)
    {
        if (_context.WorldHost.World.BlockHost.IsPosLoaded(packet.x, packet.y, packet.z))
        {
            BlockEntitySign? signEntity = _context.WorldHost.World.Entities.GetBlockEntity<BlockEntitySign>(packet.x, packet.y, packet.z);

            if (signEntity != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    signEntity.Texts[i] = packet.text[i];
                }

                signEntity.MarkDirty();
            }
        }
    }

    public override void onScreenHandlerPropertyUpdate(ScreenHandlerPropertyUpdateS2CPacket packet)
    {
        handle(packet);
        ClientPlayerEntity player = _context.PlayerHost.Player;
        if (player.currentScreenHandler != null && player.currentScreenHandler.SyncId == packet.syncId)
        {
            player.currentScreenHandler.setProperty(packet.propertyId, packet.value);
        }

    }

    public override void onEntityEquipmentUpdate(EntityEquipmentUpdateS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        ent?.setEquipmentStack(packet.slot, packet.itemRawId, packet.itemDamage);

    }

    public override void onCloseScreen(CloseScreenS2CPacket packet)
    {
        _context.PlayerHost.Player.closeHandledScreen();
    }

    public override void onPlayNoteSound(PlayNoteSoundS2CPacket packet)
    {
        _context.WorldHost.World?.Broadcaster.PlayNote(packet.xLocation, packet.yLocation, packet.zLocation, packet.instrumentType, packet.pitch);
    }

    public override void onGameStateChange(GameStateChangeS2CPacket packet)
    {
        int reason = packet.reason;
        if (reason >= 0 && reason < GameStateChangeS2CPacket.REASONS.Length && GameStateChangeS2CPacket.REASONS[reason] != null)
        {
            _context.PlayerHost.Player.sendMessage(GameStateChangeS2CPacket.REASONS[reason]);
        }

        if (reason == 1)
        {
            _worldClient.Properties.IsRaining = true;
            _worldClient.Environment.SetRainGradient(1.0F);
        }
        else if (reason == 2)
        {
            _worldClient.Properties.IsRaining = false;
            _worldClient.Environment.SetRainGradient(0.0F);
        }
        else if (reason == 7)
        {
            _worldClient.Properties.IsThundering = true;
            _worldClient.Environment.SetThunderGradient(1.0F);
        }
        else if (reason == 8)
        {
            _worldClient.Properties.IsThundering = false;
            _worldClient.Environment.SetThunderGradient(0.0F);
        }
    }

    public override void onMapUpdate(MapUpdateS2CPacket packet)
    {
        if (packet.itemRawId == Item.Map.id)
        {
            ItemMap.getMapState(packet.id, _context.WorldHost.World).UpdateData(packet.updateData);
        }
        else
        {
            _logger.LogInformation($"Unknown itemid: {packet.id}");
        }

    }

    public override void onWorldEvent(WorldEventS2CPacket packet)
    {
        _context.WorldHost.World?.Broadcaster.WorldEvent(packet.eventId, packet.x, packet.y, packet.z, packet.data);
    }

    public override void onIncreaseStat(IncreaseStatS2CPacket packet)
    {
        try
        {
            StatBase stat = Stats.Stats.GetStatById(packet.statId);
            ((EntityClientPlayerMP)_context.PlayerHost.Player).IncreaseRemoteStat(stat, packet.amount);
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
            Entity? ent = _worldClient.GetEntity(packet.entityId);
            _context.EntityRenderDispatcher.SkinManager?.Release(packet.name);
        }
    }

    public override void onRegistryData(RegistryDataS2CPacket packet)
    {
        _clientRegistries.Accumulate(packet);
    }

    public override void onFinishConfiguration(FinishConfigurationS2CPacket packet)
    {
        _logger.LogInformation("Configuration finished");
    }

    public override void onPlayerGameModeUpdate(PlayerGameModeUpdateS2CPacket packet)
    {
        Holder<GameMode>? gameMode = _clientRegistries.Get(RegistryKeys.GameModes, new ResourceLocation(packet.Namespace, packet.GameModeName));
        if (gameMode is not null && _context.PlayerHost.Player is { } player)
        {
            player.GameModeHolder = gameMode;
        }
        else if (gameMode is null)
        {
            _logger.LogWarning("Received unknown game mode name '{Name}'", packet.GameModeName);
        }
    }

    public override bool isServerSide()
    {
        return false;
    }
}
