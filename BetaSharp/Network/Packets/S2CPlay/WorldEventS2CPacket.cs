using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class WorldEventS2CPacket() : Packet(PacketId.WorldEventS2C)
{
    public int eventId;
    public int data;
    public int x;
    public int y;
    public int z;

    public static WorldEventS2CPacket Get(int eventId, int x, int y, int z, int data)
    {
        var p = Get<WorldEventS2CPacket>(PacketId.WorldEventS2C);
        p.eventId = eventId;
        p.x = x;
        p.y = y;
        p.z = z;
        p.data = data;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        eventId = stream.ReadInt();
        x = stream.ReadInt();
        y = (sbyte)stream.ReadByte();
        z = stream.ReadInt();
        data = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(eventId);
        stream.WriteInt(x);
        stream.WriteByte((byte)y);
        stream.WriteInt(z);
        stream.WriteInt(data);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onWorldEvent(this);
    }

    public override int Size()
    {
        return 20;
    }
}
