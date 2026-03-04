using System.Net.Sockets;

namespace BetaSharp.Network.Packets;

public abstract class ExtendedProtocolPacket : Packet
{
    public ExtendedProtocolPacket(byte id) : base(id) { }
    public ExtendedProtocolPacket(PacketId id) : base(id) { }

    public override void Read(NetworkStream stream)
    {
        //EntityId = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
    {
        // stream.WriteInt(EntityId);
    }
}
