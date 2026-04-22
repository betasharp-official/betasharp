using System.Net.Sockets;
using BetaSharp.Network.Packets.C2SPlay;
using BetaSharp.Network.Packets.Play;
using BetaSharp.Network.Packets.S2CPlay;
using BetaSharp.Util;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Network.Packets;

public abstract class Packet
{
    public static readonly ObjectFactoryPool<Packet, PacketRegisterItem> Registry = new(256);
    private static readonly ILogger<Packet> s_logger = Log.Instance.For<Packet>();

    private static readonly Dictionary<int, PacketTracker> s_trackers = new();

    public long CreationTime;

    public readonly byte Id;

    /// <summary>
    /// When sending to multiple clients, we only want to return when all packages have been sent
    /// </summary>
    public int UseCount;

#if DEBUG
    public string AllocationTrace = string.Empty;
    public string ReturnTrace = string.Empty;
    public bool IsReturned = false;
#endif

    protected Packet(byte id)
    {
        Id = id;
        UseCount = 1;
    }

    protected Packet(PacketId id)
    {
        Id = (byte)id;
        UseCount = 1;
    }

    public void Return()
    {
        Interlocked.Decrement(ref UseCount);
        ReturnNoCount();
    }

    protected void ReturnNoCount()
    {
        if (Volatile.Read(ref UseCount) > 0) return;

#if DEBUG
        if (IsReturned)
        {
            throw new InvalidOperationException($"Packet double-returned! It was freed by an earlier call.\nAllocated at:\n{AllocationTrace}\n\nPreviously returned at:\n{ReturnTrace}\n\nDouble return at:\n{Environment.StackTrace}");
        }
        IsReturned = true;
        ReturnTrace = Environment.StackTrace;
#endif

        if (Registry.TryGet(Id, out PacketRegisterItem? item))
        {
            item.Return(this);
            return;
        }

        s_logger.LogError("Packet id " + Id + " not found");
    }

    public static T Get<T>(PacketId id) where T : Packet => (T)Get((byte)id);

    public Packet Get() => Get(Id);
    public static Packet Get(PacketId id) => Get((byte)id);

    public static Packet Get(byte id)
    {
        if (!Registry.TryGet(id, out PacketRegisterItem? packetR))
        {
            throw new Exception("Unable to get packet id " + id);
        }

        return packetR.Get();
    }

    public static Packet? Read(Stream stream, bool server)
    {
        Packet? packet;
        int rawId;
        try
        {
            rawId = stream.ReadByte();
            if (rawId == -1)
            {
                return null;
            }

            if (!Registry.TryGet(rawId, out PacketRegisterItem? packetR))
            {
                throw new IOException("Bad packet id " + rawId);
            }

            if (server)
            {
                if (!packetR.ServerBound) throw new IOException("Bad server bound packet id " + rawId);
            }
            else
            {
                if (!packetR.ClientBound) throw new IOException("Bad client bound packet id " + rawId);
            }

            packet = packetR.Get();

            packet.Read(stream);
        }
        catch (IOException e)
        {
            s_logger.LogInformation("Reached end of stream : " + e.Message);
            return null;
        }

        if (!s_trackers.TryGetValue(rawId, out PacketTracker? tracker))
        {
            tracker = new PacketTracker();
            s_trackers.Add(rawId, tracker);
        }

        tracker.update(packet.Size());

        return packet;
    }

    public static void Write(Packet packet, Stream stream)
    {
#if DEBUG
        if (packet.IsReturned)
        {
            throw new InvalidOperationException($"Packet used after return (Write). Allocated at:\n{packet.AllocationTrace}\n\nReturned at:\n{packet.ReturnTrace}\n\nWritten at:\n{Environment.StackTrace}");
        }
#endif
        stream.WriteByte((byte)packet.Id);
        packet.Write(stream);
        packet.Return();
    }

    public abstract void Read(Stream stream);

    public abstract void Write(Stream stream);

    public abstract void Apply(NetHandler handler);

    public abstract int Size();

    public virtual void ProcessForInternal() { }

    static Packet()
    {
        Registry.Register([
            New(PacketId.KeepAlive, true, true, false, () => new KeepAlivePacket()),
            New(PacketId.LoginHello, true, true, false, () => new LoginHelloPacket()),
            New(PacketId.Handshake, true, true, false, () => new HandshakePacket()),
            New(PacketId.ChatMessage, true, true, false, () => new ChatMessagePacket()),
            New(PacketId.WorldTimeUpdateS2C, true, false, false, () => new WorldTimeUpdateS2CPacket()),
            New(PacketId.EntityEquipmentUpdateS2C, true, false, false, () => new EntityEquipmentUpdateS2CPacket()),
            New(PacketId.PlayerSpawnPositionS2C, true, false, false, () => new PlayerSpawnPositionS2CPacket()),
            New(PacketId.PlayerInteractEntityC2S, false, true, false, () => new PlayerInteractEntityC2SPacket()),
            New(PacketId.HealthUpdateS2C, true, false, false, () => new HealthUpdateS2CPacket()),
            New(PacketId.PlayerRespawn, true, true, false, () => new PlayerRespawnPacket()),
            New(PacketId.PlayerMove, true, true, false, () => new PlayerMovePacket()),
            New(PacketId.PlayerMovePositionAndOnGround, true, true, false, () => new PlayerMovePositionAndOnGroundPacket()),
            New(PacketId.PlayerMoveLookAndOnGround, true, true, false, () => new PlayerMoveLookAndOnGroundPacket()),
            New(PacketId.PlayerMoveFull, true, true, false, () => new PlayerMoveFullPacket()),
            New(PacketId.PlayerActionC2S, false, true, false, () => new PlayerActionC2SPacket()),
            New(PacketId.PlayerInteractBlockC2S, false, true, false, () => new PlayerInteractBlockC2SPacket()),
            New(PacketId.UpdateSelectedSlotC2S, false, true, false, () => new UpdateSelectedSlotC2SPacket()),
            New(PacketId.PlayerSleepUpdateS2C, true, false, false, () => new PlayerSleepUpdateS2CPacket()),
            New(PacketId.EntityAnimation, true, true, false, () => new EntityAnimationPacket()),
            New(PacketId.ClientCommandC2S, false, true, false, () => new ClientCommandC2SPacket()),
            New(PacketId.PlayerSpawnS2C, true, false, false, () => new PlayerSpawnS2CPacket()),
            New(PacketId.ItemEntitySpawnS2C, true, false, false, () => new ItemEntitySpawnS2CPacket()),
            New(PacketId.ItemPickupAnimationS2C, true, false, false, () => new ItemPickupAnimationS2CPacket()),
            New(PacketId.EntitySpawnS2C, true, false, false, () => new EntitySpawnS2CPacket()),
            New(PacketId.LivingEntitySpawnS2C, true, false, false, () => new LivingEntitySpawnS2CPacket()),
            New(PacketId.PaintingEntitySpawnS2C, true, false, false, () => new PaintingEntitySpawnS2CPacket()),
            New(PacketId.PlayerInputC2S, false, true, false, () => new PlayerInputC2SPacket()),
            New(PacketId.EntityVelocityUpdateS2C, true, false, false, () => new EntityVelocityUpdateS2CPacket()),
            New(PacketId.EntityDestroyS2C, true, false, false, () => new EntityDestroyS2CPacket()),
            New(PacketId.EntityS2C, true, false, false, () => new EntityS2CPacket()),
            New(PacketId.EntityMoveRelativeS2C, true, false, false, () => new EntityMoveRelativeS2CPacket()),
            New(PacketId.EntityRotateS2C, true, false, false, () => new EntityRotateS2CPacket()),
            New(PacketId.EntityRotateAndMoveRelativeS2C, true, false, false, () => new EntityRotateAndMoveRelativeS2CPacket()),
            New(PacketId.EntityPositionS2C, true, false, false, () => new EntityPositionS2CPacket()),
            New(PacketId.EntityStatusS2C, true, false, false, () => new EntityStatusS2CPacket()),
            New(PacketId.EntityVehicleSetS2C, true, false, false, () => new EntityVehicleSetS2CPacket()),
            New(PacketId.EntityTrackerUpdateS2C, true, false, false, () => new EntityTrackerUpdateS2CPacket()),
            New(PacketId.ChunkStatusUpdateS2C, true, false, false, () => new ChunkStatusUpdateS2CPacket()),
            New(PacketId.ChunkDataS2C, true, false, true, () => new ChunkDataS2CPacket()),
            New(PacketId.ChunkDeltaUpdateS2C, true, false, true, () => new ChunkDeltaUpdateS2CPacket()),
            New(PacketId.BlockUpdateS2C, true, false, false, () => new BlockUpdateS2CPacket()),
            New(PacketId.PlayNoteSoundS2C, true, false, false, () => new PlayNoteSoundS2CPacket()),
            New(PacketId.ExplosionS2C, true, false, false, () => new ExplosionS2CPacket()),
            New(PacketId.WorldEventS2C, true, false, false, () => new WorldEventS2CPacket()),
            New(PacketId.GameStateChangeS2C, true, false, false, () => new GameStateChangeS2CPacket()),
            New(PacketId.GlobalEntitySpawnS2C, true, false, false, () => new GlobalEntitySpawnS2CPacket()),
            New(PacketId.OpenScreenS2C, true, false, false, () => new OpenScreenS2CPacket()),
            New(PacketId.CloseScreenS2C, true, true, false, () => new CloseScreenS2CPacket()),
            New(PacketId.ClickSlotC2S, false, true, false, () => new ClickSlotC2SPacket()),
            New(PacketId.ScreenHandlerSlotUpdateS2C, true, false, false, () => new ScreenHandlerSlotUpdateS2CPacket()),
            New(PacketId.InventoryS2C, true, false, false, () => new InventoryS2CPacket()),
            New(PacketId.ScreenHandlerPropertyUpdateS2C, true, false, false, () => new ScreenHandlerPropertyUpdateS2CPacket()),
            New(PacketId.ScreenHandlerAcknowledgement, true, true, false, () => new ScreenHandlerAcknowledgementPacket()),
            New(PacketId.UpdateSign, true, true, true, () => new UpdateSignPacket()),
            New(PacketId.MapUpdateS2C, true, false, true, () => new MapUpdateS2CPacket()),
            New(PacketId.PlayerConnectionUpdateS2C, true, false, false, () => new PlayerConnectionUpdateS2CPacket()),
            New(PacketId.PlayerGameModeUpdateS2C, true, false, false, () => new PlayerGameModeUpdateS2CPacket()),
            New(PacketId.RegistryDataS2C, true, false, false, () => new RegistryDataS2CPacket()),
            New(PacketId.FinishConfigurationS2C, true, false, false, () => new FinishConfigurationS2CPacket()),
            New(PacketId.BundleS2C, true, false, false, () => new BundleS2CPacket()),
            New(PacketId.IncreaseStatS2C, true, false, false, () => new IncreaseStatS2CPacket()),
            New(PacketId.Disconnect, true, true, false, () => new DisconnectPacket())
        ]);
    }

    public class PacketRegisterItem(byte rawId, bool clientBound, bool serverBound, bool worldPacket, Func<Packet> factory) : FactoryPoolItem<Packet>(rawId, item: factory, PoolSize(rawId))
    {
        private static int PoolSize(byte rawId)
        {
            return rawId switch
            {
                (byte)PacketId.EntityRotateAndMoveRelativeS2C or (byte)PacketId.EntityMoveRelativeS2C or (byte)PacketId.EntityPositionS2C => 256,
                (byte)PacketId.ChunkStatusUpdateS2C or (byte)PacketId.BlockUpdateS2C or (byte)PacketId.PlayerActionC2S or (byte)PacketId.ChunkDataS2C or (byte)PacketId.LivingEntitySpawnS2C or (byte)PacketId.EntityDestroyS2C => 64,
                (byte)PacketId.KeepAlive or (byte)PacketId.UpdateSign or (byte)PacketId.PlayerConnectionUpdateS2C or (byte)PacketId.PlayerGameModeUpdateS2C or (byte)PacketId.Disconnect or (byte)PacketId.LoginHello or (byte)PacketId.Handshake or (byte)PacketId.ChatMessage or (byte)PacketId.EntityEquipmentUpdateS2C or (byte)PacketId.PlayerSpawnPositionS2C or (byte)PacketId.PaintingEntitySpawnS2C => 16,
                _ => 32,
            };
        }

        public override Packet Get()
        {
            Packet p = Item.Get();
            // note. DateTimeOffset.UtcNow.UtcTicks would be slightly faster as no conversion would be needed
            p.CreationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            p.UseCount = 1;
#if DEBUG
            p.IsReturned = false;
            p.AllocationTrace = Environment.StackTrace;
            p.ReturnTrace = string.Empty;
#endif
            return p;
        }

        public readonly bool ClientBound = clientBound;
        public readonly bool ServerBound = serverBound;
        public readonly bool WorldPacket = worldPacket;
    }

    private static PacketRegisterItem New(PacketId rawId, bool clientBound, bool serverBound, bool worldPacket, Func<Packet> factory) =>
        new((byte)rawId, clientBound, serverBound, worldPacket, factory);
}
