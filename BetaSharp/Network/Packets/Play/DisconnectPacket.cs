using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class DisconnectPacket() : Packet(PacketId.Disconnect)
{
    public string reason;

    public DisconnectPacket(string reason) : this()
    {
        this.reason = reason;
    }

    public override void Read(NetworkStream stream)
    {
        reason = stream.ReadLongString(100);
    }

    public override void Write(NetworkStream stream)
    {
        // TODO: should have a index for common responses
        stream.WriteLongString(reason);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onDisconnect(this);
    }

    public override int Size()
    {
        return reason.Length;
    }
}
