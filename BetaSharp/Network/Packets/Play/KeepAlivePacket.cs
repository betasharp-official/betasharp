using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class KeepAlivePacket() : Packet(PacketId.KeepAlive)
{
    public override void Apply(NetHandler handler) { }

    public override void Read(NetworkStream stream) { }

    public override void Write(NetworkStream stream) { }

    public override int Size()
    {
        return 0;
    }
}
