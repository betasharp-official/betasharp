using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class WorldEventS2CPacket() : Packet(PacketId.WorldEventS2C)
{
    public int eventId;
    public int data;
    public int x;
    public int y;
    public int z;

    public WorldEventS2CPacket(int eventId, int x, int y, int z, int data) : this()
    {
        this.eventId = eventId;
        this.x = x;
        this.y = y;
        this.z = z;
        this.data = data;
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
