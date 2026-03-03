using System.Net.Sockets;

namespace BetaSharp.Network.Packets;

public abstract class PacketBaseEntity : Packet
{
    protected const int PacketBaseEntitySize = 4;

    public int EntityId { get; set; }

    public PacketBaseEntity(byte id) : base(id) { }
    public PacketBaseEntity(PacketId id) : base(id) { }

    public override void Read(NetworkStream stream)
    {
        EntityId = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(EntityId);
    }

    public override int Size()
    {
        return PacketBaseEntitySize;
    }
}
