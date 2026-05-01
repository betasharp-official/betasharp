namespace BetaSharp.Network.Packets.S2CPlay;

public class CloseScreenS2CPacket() : Packet(PacketId.CloseScreenS2C)
{
    private int _windowId;

    public static CloseScreenS2CPacket Get(int windowId)
    {
        CloseScreenS2CPacket p = Get<CloseScreenS2CPacket>(PacketId.CloseScreenS2C);
        p._windowId = windowId;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onCloseScreen(this);

    public override void Read(Stream stream) => _windowId = (sbyte)stream.ReadByte();

    public override void Write(Stream stream) => stream.WriteByte((byte)_windowId);

    public override int Size() => 1;
}
