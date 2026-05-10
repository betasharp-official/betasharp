namespace BetaSharp.Network.Packets.Play;

public class KeepAlivePacket() : Packet(PacketId.KeepAlive)
{
    public static KeepAlivePacket Get() => Get<KeepAlivePacket>(PacketId.KeepAlive);

    public override void Apply(NetHandler handler) { }

    public override void Read(Stream stream) { }

    public override void Write(Stream stream) { }

    public override int Size() => 0;
}
