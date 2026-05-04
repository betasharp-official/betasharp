namespace BetaSharp.Network.Packets;

public class HandshakePacket() : Packet(PacketId.Handshake)
{
    private string Username { get; set; } = "";

    public static HandshakePacket Get(string username)
    {
        HandshakePacket p = Get<HandshakePacket>(PacketId.Handshake);
        p.Username = username;
        return p;
    }

    public override void Read(Stream stream) => Username = stream.ReadLongString(32);

    public override void Write(Stream stream) => stream.WriteLongString(Username);

    public override void Apply(NetHandler handler) => handler.onHandshake(this);

    public override int Size() => 4 + Username.Length + 4;
}
