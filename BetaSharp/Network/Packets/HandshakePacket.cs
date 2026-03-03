using System.Net.Sockets;

namespace BetaSharp.Network.Packets;

public class HandshakePacket() : Packet(PacketId.Handshake)
{
    public string username;

    public HandshakePacket(string username) : this()
    {
        this.username = username;
    }

    public override void Read(NetworkStream stream)
    {
        username = stream.ReadLongString(32);
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteLongString(username);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onHandshake(this);
    }

    public override int Size()
    {
        return 4 + username.Length + 4;
    }
}
