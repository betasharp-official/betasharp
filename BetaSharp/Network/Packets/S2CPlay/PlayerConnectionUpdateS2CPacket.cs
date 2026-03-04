using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerConnectionUpdateS2CPacket() : ExtendedProtocolPacket(PacketId.PlayerConnectionUpdateS2C)
{
    public enum ConnectionUpdateType : byte
    {
        Join = 0,
        Leave = 1
    }

    public int entityId;
    public ConnectionUpdateType type;
    public string name;

    public PlayerConnectionUpdateS2CPacket(
        int entityId,
        ConnectionUpdateType type,
        string name
    ) : this()
    {
        this.entityId = entityId;
        this.type = type;
        this.name = name;
    }

    public override void Read(NetworkStream stream)
    {
        entityId = stream.ReadInt();
        type = (ConnectionUpdateType)stream.ReadByte();
        name = stream.ReadLongString(16);
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteByte((byte)type);
        stream.WriteLongString(name);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerConnectionUpdate(this);
    }

    public override int Size()
    {
        return 39;
    }
}
