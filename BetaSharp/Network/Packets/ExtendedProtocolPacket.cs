namespace BetaSharp.Network.Packets;

public abstract class ExtendedProtocolPacket : Packet
{
    public ExtendedProtocolPacket(byte id) : base(id) { }
    public ExtendedProtocolPacket(PacketId id) : base(id) { }
}
