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

    public static PlayerConnectionUpdateS2CPacket Get(
        int entityId,
        ConnectionUpdateType type,
        string name
    )
    {
        var p = Get<PlayerConnectionUpdateS2CPacket>(PacketId.PlayerConnectionUpdateS2C);
        p.entityId = entityId;
        p.type = type;
        p.name = name;
        return p;
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
