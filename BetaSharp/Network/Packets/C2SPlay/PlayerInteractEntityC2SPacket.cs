namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInteractEntityC2SPacket() : Packet(PacketId.PlayerInteractEntityC2S), IPacketEntity
{
    private int _playerId;
    public int IsLeftClick { get; private set; }
    public int EntityId { get; private set; }

    public static PlayerInteractEntityC2SPacket Get(int playerId, int entityId, int isLeftClick)
    {
        PlayerInteractEntityC2SPacket p = Get<PlayerInteractEntityC2SPacket>(PacketId.PlayerInteractEntityC2S);
        p._playerId = playerId;
        p.EntityId = entityId;
        p.IsLeftClick = isLeftClick;
        return p;
    }

    public override void Read(Stream stream)
    {
        _playerId = stream.ReadInt();
        EntityId = stream.ReadInt();
        IsLeftClick = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(_playerId);
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)IsLeftClick);
    }

    public override void Apply(NetHandler handler) => handler.handleInteractEntity(this);

    public override int Size() => 9;
}
