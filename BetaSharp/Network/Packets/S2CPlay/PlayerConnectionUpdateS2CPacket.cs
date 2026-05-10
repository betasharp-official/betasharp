namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerConnectionUpdateS2CPacket() : ExtendedProtocolPacket(PacketId.PlayerConnectionUpdateS2C), IPacketEntity
{
    public enum ConnectionUpdateType : byte
    {
        Join = 0,
        Leave = 1
    }

    public string Name { get; private set; } = "";
    public ConnectionUpdateType Type { get; private set; }

    public int EntityId { get; private set; }

    public static PlayerConnectionUpdateS2CPacket Get(
        int entityId,
        ConnectionUpdateType type,
        string name
    )
    {
        PlayerConnectionUpdateS2CPacket p = Get<PlayerConnectionUpdateS2CPacket>(PacketId.PlayerConnectionUpdateS2C);
        p.EntityId = entityId;
        p.Type = type;
        p.Name = name;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Type = (ConnectionUpdateType)stream.ReadByte();
        Name = stream.ReadLongString(16);
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)Type);
        stream.WriteLongString(Name);
    }

    public override void Apply(NetHandler handler) => handler.onPlayerConnectionUpdate(this);

    public override int Size() => 39;
}
