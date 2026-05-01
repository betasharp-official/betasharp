namespace BetaSharp.Network.Packets.Play;

public class DisconnectPacket() : Packet(PacketId.Disconnect)
{
    public string Reason { get; private set; } = "";

    public static DisconnectPacket Get(string reason)
    {
        DisconnectPacket p = Get<DisconnectPacket>(PacketId.Disconnect);
        p.Reason = reason;
        return p;
    }

    public override void Read(Stream stream) => Reason = stream.ReadLongString(100);

    public override void Write(Stream stream) =>
        // TODO: should have a index for common responses
        stream.WriteLongString(Reason);

    public override void Apply(NetHandler handler) => handler.onDisconnect(this);

    public override int Size() => Reason.Length;
}
