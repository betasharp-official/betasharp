using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkStatusUpdateS2CPacket() : Packet(PacketId.ChunkStatusUpdateS2C)
{
    public int x;
    public int z;
    public bool load;

    public static ChunkStatusUpdateS2CPacket Get(int x, int z, bool load)
    {
        var p = Get<ChunkStatusUpdateS2CPacket>(PacketId.ChunkStatusUpdateS2C);
        p.x = x;
        p.z = z;
        p.load = load;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadInt();
        z = stream.ReadInt();
        load = stream.ReadByte() != 0;
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(x);
        stream.WriteInt(z);
        stream.WriteBoolean(load);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onChunkStatusUpdate(this);
    }

    public override int Size()
    {
        return 9;
    }
}
