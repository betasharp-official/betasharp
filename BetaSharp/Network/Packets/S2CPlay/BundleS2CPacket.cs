using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class BundleS2CPacket() : ExtendedProtocolPacket(PacketId.BundleS2C)
{
    public List<Packet> Packets { get; } = [];

    public override void Read(Stream stream)
    {
        int count = stream.ReadUShort();
        for (int i = 0; i < count; i++)
        {
            Packet? p = Read(stream, false); // Client bound
            if (p != null) Packets.Add(p);
        }
    }

    public override void Write(Stream stream)
    {
        stream.WriteUShort((ushort)Packets.Count);
        foreach (Packet p in Packets)
        {
            Write(p, stream);
        }
        Packets.Clear();
    }

    public override void Apply(NetHandler handler)
    {
        foreach (Packet p in Packets)
        {
            p.Apply(handler);
            p.Return();
        }
        Packets.Clear();
    }

    public override int Size()
    {
        int size = 2; // count
        foreach (Packet p in Packets)
        {
            size += 1 + p.Size(); // id + data length
        }
        return size;
    }
}
