using System.Net;
using System.Net.Sockets;
using BetaSharp.Blocks;
using BetaSharp.Blocks.Entities;
using BetaSharp.Client.Diagnostics;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Entities.FX;
using BetaSharp.Client.Rendering.Entities;
using BetaSharp.Client.Rendering.Particles;
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
        _logger.LogInformation($"[Client] Received onHello from server (id: {packet.ProtocolVersion})");
        _context.PlayerHost.SetPlayerController(_context.Factory.CreatePlayerController(this));
        _context.StatFileWriter.ReadStat(Stats.Stats.JoinMultiplayerStat, 1);
        _worldClient = new ClientWorld(this, packet.WorldSeed, packet.DimensionId)
        {
            IsRemote = true
        };
        _context.WorldHost.ChangeWorld(_worldClient);
        _context.PlayerHost.Player.DimensionId = packet.DimensionId;
        _context.Navigator.Navigate(_context.Factory.CreateTerrainScreen(this));
        _context.PlayerHost.Player.ID = packet.ProtocolVersion;
    }

    public override void onItemEntitySpawn(ItemEntitySpawnS2CPacket packet)
    {
        double x = packet.X / 32.0D;
        double y = packet.Y / 32.0D;
        double z = packet.Z / 32.0D;
        EntityItem entityItem = new(_worldClient, x, y, z, new ItemStack(packet.ItemRawId, packet.ItemCount, packet.ItemDamage))
        {
            VelocityX = packet.VelocityX / 128.0D,
            VelocityY = packet.VelocityY / 128.0D,
            VelocityZ = packet.VelocityZ / 128.0D,
            TrackedPosX = packet.X,
            TrackedPosY = packet.Y,
            TrackedPosZ = packet.Z
        };
        _worldClient.ForceEntity(packet.EntityId, entityItem);
    }

    public override void onEntitySpawn(EntitySpawnS2CPacket packet)
    {
        double x = packet.X / 32.0D;
        double y = packet.Y / 32.0D;
        double z = packet.Z / 32.0D;
        object? entity = null;
        if (packet.EntityType == 10)
        {
            entity = new EntityMinecart(_worldClient, x, y, z, 0);
        }

        if (packet.EntityType == 11)
        {
            entity = new EntityMinecart(_worldClient, x, y, z, 1);
        }

        if (packet.EntityType == 12)
        {
            entity = new EntityMinecart(_worldClient, x, y, z, 2);
        }

        if (packet.EntityType == 90)
        {
            entity = new EntityFish(_worldClient, x, y, z);
        }

        if (packet.EntityType == 60)
        {
            entity = new EntityArrow(_worldClient, x, y, z);
        }

        if (packet.EntityType == 61)
        {
            entity = new EntitySnowball(_worldClient, x, y, z);
        }

        if (packet.EntityType == 63)
        {
            entity = new EntityFireball(_worldClient, x, y, z, packet.VelocityX / 8000.0D, packet.VelocityY / 8000.0D, packet.VelocityZ / 8000.0D);
            packet.EntityData = 0;
        }

        if (packet.EntityType == 62)
        {
            entity = new EntityEgg(_worldClient, x, y, z);
        }

        if (packet.EntityType == 1)
        {
            entity = new EntityBoat(_worldClient, x, y, z);
        }

        if (packet.EntityType == 50)
        {
            entity = new EntityTntPrimed(_worldClient, x, y, z);
        }

        if (packet.EntityType == 70)
        {
            entity = new EntityFallingSand(_worldClient, x, y, z, Block.Sand.id);
        }

        if (packet.EntityType == 71)
        {
            entity = new EntityFallingSand(_worldClient, x, y, z, Block.Gravel.id);
        }

        if (entity != null)
        {
            ((Entity)entity).TrackedPosX = packet.X;
            ((Entity)entity).TrackedPosY = packet.Y;
            ((Entity)entity).TrackedPosZ = packet.Z;
            ((Entity)entity).Yaw = 0.0F;
            ((Entity)entity).Pitch = 0.0F;
            ((Entity)entity).ID = packet.EntityId;
            _worldClient.ForceEntity(packet.EntityId, (Entity)entity);
            if (packet.EntityData > 0)
            {
                if (packet.EntityType == 60)
                {
                    Entity? owner = GetEntityById(packet.EntityData);
                    if (owner is EntityLiving)
                    {
                        ((EntityArrow)entity).Owner = (EntityLiving)owner;
                    }
                }

                ((Entity)entity).SetVelocityClient(packet.VelocityX / 8000.0D, packet.VelocityY / 8000.0D, packet.VelocityZ / 8000.0D);
            }
        }

    }

    public override void onLightningEntitySpawn(GlobalEntitySpawnS2CPacket packet)
    {
        double x = packet.X / 32.0D;
        double y = packet.Y / 32.0D;
        double z = packet.Z / 32.0D;
        EntityLightningBolt? ent = null;
        if (packet.Type == 1)
        {
            ent = new EntityLightningBolt(_worldClient, x, y, z);
        }

        if (ent != null)
        {
            ent.TrackedPosX = packet.X;
            ent.TrackedPosY = packet.Y;
            ent.TrackedPosZ = packet.Z;
            ent.Yaw = 0.0F;
            ent.Pitch = 0.0F;
            ent.ID = packet.EntityId;
            _worldClient.Entities.SpawnGlobalEntity(ent);
        }

    }

    public override void onPaintingEntitySpawn(PaintingEntitySpawnS2CPacket packet)
    {
        EntityPainting ent = new(_worldClient, packet.XPosition, packet.YPosition, packet.ZPosition, packet.Direction, packet.Title);
        _worldClient.ForceEntity(packet.EntityId, ent);
    }

    public override void onEntityVelocityUpdate(EntityVelocityUpdateS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        ent?.SetVelocityClient(packet.MotionX / 8000.0D, packet.MotionY / 8000.0D, packet.MotionZ / 8000.0D);
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
        double x = packet.XPosition / 32.0D;
        double y = packet.YPosition / 32.0D;
        double z = packet.ZPosition / 32.0D;
        float rotation = packet.Rotation * 360 / 256.0F;
        float pitch = packet.Pitch * 360 / 256.0F;
        OtherPlayerEntity ent = new(_context.WorldHost.World, packet.Name);
        ent.PrevX = ent.LastTickX = ent.TrackedPosX = packet.XPosition;
        ent.PrevY = ent.LastTickY = ent.TrackedPosY = packet.YPosition;
        ent.PrevZ = ent.LastTickZ = ent.TrackedPosZ = packet.ZPosition;
        int currentItem = packet.CurrentItem;
        if (currentItem == 0)
        {
            ent.Inventory.Main[ent.Inventory.SelectedSlot] = null;
        }
        else
        {
            ent.Inventory.Main[ent.Inventory.SelectedSlot] = new ItemStack(currentItem, 1, 0);
        }

        ent.SetPositionAndAngles(x, y, z, rotation, pitch);
        _worldClient.ForceEntity(packet.EntityId, ent);
    }

    public override void onEntityPosition(EntityPositionS2CPacket packet)
    {
        Entity ent = GetEntityById(packet);
        if (ent != null)
        {
            ent.TrackedPosX = packet.X;
            ent.TrackedPosY = packet.Y;
            ent.TrackedPosZ = packet.Z;
            float yaw = packet.Yaw * 360 / 256.0F;
            float pitch = packet.Pitch * 360 / 256.0F;
            ent.SetPositionAndAnglesAvoidEntities(yaw, pitch, 5);
        }
    }

    public override void onEntity(EntityS2CPacket packet)
    {
        Entity ent = GetEntityById(packet);
        if (ent != null)
        {
            ent.TrackedPosX += packet.DeltaX;
            ent.TrackedPosY += packet.DeltaY;
            ent.TrackedPosZ += packet.DeltaZ;
            float yaw = packet.Rotate ? packet.Yaw * 360 / 256.0F : ent.Yaw;
            float pitch = packet.Rotate ? packet.Pitch * 360 / 256.0F : ent.Pitch;
            ent.SetPositionAndAnglesAvoidEntities(yaw, pitch, 5);
        }
    }

    public override void onEntity(EntityRotateS2CPacket packet)
    {
        Entity ent = GetEntityById(packet);
        if (ent != null)
        {
            float yaw = packet.Yaw * 360 / 256.0F;
            float pitch = packet.Pitch * 360 / 256.0F;
            ent.SetPositionAndAnglesAvoidEntities(yaw, pitch, 5);
        }
    }

    public override void onEntity(EntityMoveRelativeS2CPacket s2CPacket)
    {
        Entity ent = GetEntityById(s2CPacket);
        if (ent != null)
        {
            ent.TrackedPosX += s2CPacket.DeltaX;
            ent.TrackedPosY += s2CPacket.DeltaY;
            ent.TrackedPosZ += s2CPacket.DeltaZ;
            ent.SetPositionAndAnglesAvoidEntities(5);
        }
    }

    public override void onEntity(EntityRotateAndMoveRelativeS2CPacket s2CPacket)
    {
        Entity ent = GetEntityById(s2CPacket);
        if (ent != null)
        {
            ent.TrackedPosX += s2CPacket.DeltaX;
            ent.TrackedPosY += s2CPacket.DeltaY;
            ent.TrackedPosZ += s2CPacket.DeltaZ;
            float yaw = s2CPacket.Yaw * 360 / 256.0F;
            float pitch = s2CPacket.Pitch * 360 / 256.0F;
            ent.SetPositionAndAnglesAvoidEntities(yaw, pitch, 5);
        }
    }

    public override void onEntityDestroy(EntityDestroyS2CPacket packet)
    {
        _worldClient.RemoveEntityFromWorld(packet.EntityId);
    }

    public override void onPlayerMove(PlayerMovePacket packet)
    {
        ClientPlayerEntity ent = _context.PlayerHost.Player;
        double x = ent.X;
        double y = ent.Y;
        double z = ent.Z;
        float yaw = ent.Yaw;
        float pitch = ent.Pitch;
        if (packet.ChangePosition)
        {
            x = packet.X;
            y = packet.Y;
            z = packet.Z;
        }

        if (packet.ChangeLook)
        {
            yaw = packet.Yaw;
            pitch = packet.Pitch;
        }

        ent.CameraOffset = 0.0F;
        ent.VelocityX = ent.VelocityY = ent.VelocityZ = 0.0D;
        ent.SetPositionAndAngles(x, y, z, yaw, pitch);
        packet.X = ent.X;
        packet.Y = ent.BoundingBox.MinY;
        packet.Z = ent.Z;
        packet.EyeHeight = ent.Y;
        SendPacket(packet);
        if (!_terrainLoaded)
        {
            ClientPlayerEntity player = _context.PlayerHost.Player;
            player.PrevX = player.X;
            player.PrevY = player.Y;
            player.PrevZ = player.Z;
            _terrainLoaded = true;
            _context.Navigator.Navigate(null);
        }

    }

    public override void onChunkStatusUpdate(ChunkStatusUpdateS2CPacket packet)
    {
        _worldClient.UpdateChunk(packet.X, packet.Z, packet.Load);
    }

    public override void onChunkDeltaUpdate(ChunkDeltaUpdateS2CPacket packet)
    {
        Chunk chunk = _worldClient.BlockHost.GetChunk(packet.X, packet.Z);
        int x = packet.X * 16;
        int y = packet.Z * 16;

        for (int i = 0; i < packet.Count; ++i)
        {
            short positions = packet.Positions[i];
            int blockRawId = packet.BlockRawIds[i] & 255;
            byte metadata = packet.BlockMetadata[i];
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
        _worldClient.ClearBlockResets(packet.X, packet.Y, packet.Z, packet.X + packet.SizeX - 1, packet.Y + packet.SizeY - 1, packet.Z + packet.SizeZ - 1);
        _worldClient.HandleChunkDataUpdate(packet.X, packet.Y, packet.Z, packet.SizeX, packet.SizeY, packet.SizeZ, packet.ChunkData);
    }

    public override void onBlockUpdate(BlockUpdateS2CPacket packet)
    {
        _worldClient.SetBlockWithMetaFromPacket(packet.X, packet.Y, packet.Z, packet.BlockRawId, packet.BlockMetadata);
    }

    public override void onDisconnect(DisconnectPacket packet)
    {
        _netManager.disconnect("disconnect.kicked");
        Disconnected = true;
        _context.WorldHost.ChangeWorld(null);
        _context.Navigator.Navigate(_context.Factory.CreateFailedScreen("disconnect.disconnected", "disconnect.genericReason", [packet.Reason]));
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
        Entity? ent = GetEntityById(packet.EntityId);
        Entity collector = GetEntityById(packet.CollectorEntityId) as EntityLiving ?? _context.PlayerHost.Player;

        if (ent != null && collector != null)
        {
            _worldClient.Broadcaster.PlaySoundAtEntity(ent, "random.pop", 0.2F, ((_rand.NextFloat() - _rand.NextFloat()) * 0.7F + 1.0F) * 2.0F);
            _context.ParticleManager.AddSpecialParticle(new LegacyParticleAdapter(new EntityPickupFX(_context.WorldHost.World, ent, collector, -0.5F)));
            _worldClient.RemoveEntityFromWorld(packet.EntityId);
        }

    }

    public override void onChatMessage(ChatMessagePacket packet)
    {
        _context.AddChatMessage(packet.ChatMessage);
    }

    public override void onEntityAnimation(EntityAnimationPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        if (ent != null)
        {
            if (packet.AnimationId == 1)
            {
                if (ent is EntityPlayer player)
                    player.SwingHand();
            }
            else if (packet.AnimationId == 2)
            {
                ent.AnimateHurt();
            }
            else if (packet.AnimationId == 3)
            {
                if (ent is EntityPlayer player)
                    player.WakeUp(false, false, false);
            }
            else if (packet.AnimationId == 4)
            {
                if (ent is EntityPlayer player)
                    player.Spawn();
            }

        }
    }

    public override void onPlayerSleepUpdate(PlayerSleepUpdateS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.PlayerId);
        if (ent is EntityPlayer player)
        {
            if (packet.Status == 0)
            {
                player.TrySleep(packet.X, packet.Y, packet.Z);
            }

        }
    }

    public override void onHandshake(HandshakePacket packet)
    {
        AddToSendQueue(LoginHelloPacket.Get(_context.Session.username, 14, LoginHelloPacket.BETASHARP_CLIENT_SIGNATURE, 0));
    }

    public void Disconnect()
    {
        Disconnected = true;
        _netManager.disconnect("disconnect.closed");
    }

    public override void onLivingEntitySpawn(LivingEntitySpawnS2CPacket packet)
    {
        double x = packet.XPosition / 32.0D;
        double y = packet.YPosition / 32.0D;
        double z = packet.ZPosition / 32.0D;
        float yaw = packet.Yaw * 360 / 256.0F;
        float pitch = packet.Pitch * 360 / 256.0F;
        EntityLiving ent = (EntityLiving)EntityRegistry.Create(packet.Type, _context.WorldHost.World);
        ent.TrackedPosX = packet.XPosition;
        ent.TrackedPosY = packet.YPosition;
        ent.TrackedPosZ = packet.ZPosition;
        ent.ID = packet.EntityId;
        ent.SetPositionAndAngles(x, y, z, yaw, pitch);
        ent.LastTickX = ent.X;
        ent.LastTickY = ent.Y;
        ent.LastTickZ = ent.Z;
        ent.InterpolateOnly = true;
        _worldClient.ForceEntity(packet.EntityId, ent);
        ent.DataSynchronizer.ApplyChanges(new MemoryStream(packet.Data));
    }

    public override void onWorldTimeUpdate(WorldTimeUpdateS2CPacket packet)
    {
        _context.WorldHost.World?.SetTime(packet.Time);
    }

    public override void onPlayerSpawnPosition(PlayerSpawnPositionS2CPacket packet)
    {
        _context.PlayerHost.Player.SetSpawnPos(new Vec3i(packet.X, packet.Y, packet.Z));
        _context.WorldHost.World?.Properties.SetSpawn(packet.X, packet.Y, packet.Z);
    }

    public override void onEntityVehicleSet(EntityVehicleSetS2CPacket packet)
    {
        object? rider = GetEntityById(packet.EntityId);
        Entity? ent = GetEntityById(packet.VehicleEntityId);
        if (packet.EntityId == _context.PlayerHost.Player.ID)
        {
            rider = _context.PlayerHost.Player;
        }

        if (rider is Entity riderEntity)
        {
            riderEntity.SetVehicle(ent);
        }
    }

    public override void onEntityStatus(EntityStatusS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        ent?.ProcessServerEntityStatus(packet.EntityStatus);

    }

    private Entity? GetEntityById(IPacketEntity entityId) => GetEntityById(entityId.EntityId);
    private Entity? GetEntityById(int entityId)
    {
        if (_context.PlayerHost.Player == null || _worldClient == null)
        {
            return null;
        }

        return entityId == _context.PlayerHost.Player.ID ? _context.PlayerHost.Player : _worldClient.GetEntity(entityId);
    }

    public override void onHealthUpdate(HealthUpdateS2CPacket packet)
    {
        _context.PlayerHost.Player.setHealth(packet.HealthMp);
    }

    public override void onPlayerRespawn(PlayerRespawnPacket packet)
    {
        if (packet.DimensionId != _context.PlayerHost.Player.DimensionId)
        {
            _terrainLoaded = false;
            _worldClient = new ClientWorld(this, _worldClient.Properties.RandomSeed, packet.DimensionId)
            {
                IsRemote = true
            };
            _context.WorldHost.ChangeWorld(_worldClient);
            _context.PlayerHost.Player.DimensionId = packet.DimensionId;
            _context.Navigator.Navigate(_context.Factory.CreateTerrainScreen(this));
        }

        _context.PlayerHost.Respawn(true, packet.DimensionId);
    }

    public override void onExplosion(ExplosionS2CPacket packet)
    {
        Explosion explosion = new(_context.WorldHost.World, null, packet.ExplosionX, packet.ExplosionY, packet.ExplosionZ, packet.ExplosionSize)
        {
            destroyedBlockPositions = packet.DestroyedBlockPositions
        };
        explosion.doExplosionB(true);
    }

    public override void onOpenScreen(OpenScreenS2CPacket packet)
    {
        ClientPlayerEntity player = _context.PlayerHost.Player;
        if (!player.GameMode.CanInteract) return;

        if (packet.ScreenHandlerId == 0)
        {
            InventoryBasic inventory = new(packet.Name, packet.SlotsCount);
            player.openChestScreen(inventory);
            player.CurrentScreenHandler.SyncId = packet.SyncId;
        }
        else if (packet.ScreenHandlerId == 2)
        {
            BlockEntityFurnace furnace = new();
            player.openFurnaceScreen(furnace);
            player.CurrentScreenHandler.SyncId = packet.SyncId;
        }
        else if (packet.ScreenHandlerId == 3)
        {
            BlockEntityDispenser dispenser = new();
            player.openDispenserScreen(dispenser);
            player.CurrentScreenHandler.SyncId = packet.SyncId;
        }
        else if (packet.ScreenHandlerId == 1)
        {
            player.openCraftingScreen(MathHelper.Floor(player.X), MathHelper.Floor(player.Y), MathHelper.Floor(player.Z));
            player.CurrentScreenHandler.SyncId = packet.SyncId;
        }

    }

    public override void onScreenHandlerSlotUpdate(ScreenHandlerSlotUpdateS2CPacket packet)
    {
        ClientPlayerEntity? player = _context.PlayerHost.Player;
        if (packet.SyncId == -1)
        {
            player.Inventory.SetCursorStack(packet.Stack);
        }
        else if (packet.SyncId == 0 && packet.Slot >= 36 && packet.Slot < 45)
        {
            ItemStack? itemStack = player.PlayerScreenHandler.GetSlot(packet.Slot).getStack();
            if (packet.Stack != null && (itemStack == null || itemStack.Count < packet.Stack.Count))
            {
                packet.Stack.AnimationTime = 5;
            }

            player.PlayerScreenHandler.setStackInSlot(packet.Slot, packet.Stack);
        }
        else if (packet.SyncId == player.CurrentScreenHandler.SyncId)
        {
            player.CurrentScreenHandler.setStackInSlot(packet.Slot, packet.Stack);
        }

    }

    public override void onScreenHandlerAcknowledgement(ScreenHandlerAcknowledgementPacket packet)
    {
        ClientPlayerEntity player = _context.PlayerHost.Player;
        ScreenHandler? screenHandler = null;
        if (packet.SyncId == 0)
        {
            screenHandler = player.PlayerScreenHandler;
        }
        else if (packet.SyncId == player.CurrentScreenHandler.SyncId)
        {
            screenHandler = player.CurrentScreenHandler;
        }

        if (screenHandler != null)
        {
            if (packet.Accepted)
            {
                ScreenHandler.onAcknowledgementAccepted(packet.ActionType);
            }
            else
            {
                ScreenHandler.onAcknowledgementDenied(packet.ActionType);
                AddToSendQueue(ScreenHandlerAcknowledgementPacket.Get(packet.SyncId, packet.ActionType, true));
            }
        }

    }

    public override void onInventory(InventoryS2CPacket packet)
    {
        ClientPlayerEntity? player = _context.PlayerHost.Player;
        if (packet.SyncId == 0)
        {
            player.PlayerScreenHandler.updateSlotStacks(packet.Contents);
        }
        else if (packet.SyncId == player.CurrentScreenHandler.SyncId)
        {
            player.CurrentScreenHandler.updateSlotStacks(packet.Contents);
        }

    }

    public override void handleUpdateSign(UpdateSignPacket packet)
    {
        if (_context.WorldHost.World.BlockHost.IsPosLoaded(packet.X, packet.Y, packet.Z))
        {
            BlockEntitySign? signEntity = _context.WorldHost.World.Entities.GetBlockEntity<BlockEntitySign>(packet.X, packet.Y, packet.Z);

            if (signEntity != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    signEntity.Texts[i] = packet.Text[i];
                }

                signEntity.MarkDirty();
            }
        }
    }

    public override void onScreenHandlerPropertyUpdate(ScreenHandlerPropertyUpdateS2CPacket packet)
    {
        handle(packet);
        ClientPlayerEntity player = _context.PlayerHost.Player;
        if (player.CurrentScreenHandler != null && player.CurrentScreenHandler.SyncId == packet.SyncId)
        {
            player.CurrentScreenHandler.setProperty(packet.PropertyId, packet.Value);
        }

    }

    public override void onEntityEquipmentUpdate(EntityEquipmentUpdateS2CPacket packet)
    {
        Entity? ent = GetEntityById(packet.EntityId);
        ent?.SetEquipmentStack(packet.Slot, packet.ItemRawId, packet.ItemDamage);

    }

    public override void onCloseScreen(CloseScreenS2CPacket packet)
    {
        _context.PlayerHost.Player.closeHandledScreen();
    }

    public override void onPlayNoteSound(PlayNoteSoundS2CPacket packet)
    {
        _context.WorldHost.World?.Broadcaster.PlayNote(packet.XLocation, packet.YLocation, packet.ZLocation, packet.InstrumentType, packet.Pitch);
    }

    public override void onGameStateChange(GameStateChangeS2CPacket packet)
    {
        int reason = packet.Reason;
        if (reason >= 0 && reason < GameStateChangeS2CPacket.Reasons.Length && GameStateChangeS2CPacket.Reasons[reason] != null)
        {
            _context.PlayerHost.Player.SendMessage(GameStateChangeS2CPacket.Reasons[reason]);
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
        if (packet.ItemRawId == Item.Map.id)
        {
            ItemMap.getMapState(packet.MapId, _context.WorldHost.World).UpdateData(packet.UpdateData);
        }
        else
        {
            _logger.LogInformation($"Unknown itemid: {packet.MapId}");
        }

    }

    public override void onWorldEvent(WorldEventS2CPacket packet)
    {
        _context.WorldHost.World?.Broadcaster.WorldEvent(packet.EventId, packet.X, packet.Y, packet.Z, packet.Data);
    }

    public override void onIncreaseStat(IncreaseStatS2CPacket packet)
    {
        try
        {
            StatBase stat = Stats.Stats.GetStatById(packet.StatId);
            ((EntityClientPlayerMP)_context.PlayerHost.Player).IncreaseRemoteStat(stat, packet.Amount);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Unknown stat id in IncreaseStatS2CPacket: {StatId}", packet.StatId);
        }
    }

    public override void onPlayerConnectionUpdate(PlayerConnectionUpdateS2CPacket packet)
    {
        if (packet.Type == PlayerConnectionUpdateS2CPacket.ConnectionUpdateType.Leave)
        {
            Entity? ent = _worldClient.GetEntity(packet.EntityId);
            EntityRenderDispatcher.Instance.SkinManager?.Release(packet.Name);
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
