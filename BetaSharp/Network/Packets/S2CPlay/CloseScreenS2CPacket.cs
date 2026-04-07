using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class CloseScreenS2CPacket() : Packet(PacketId.CloseScreenS2C)
{
    public int windowId;

    public static CloseScreenS2CPacket Get(int windowId)
    {
        var p = Get<CloseScreenS2CPacket>(PacketId.CloseScreenS2C);
        p.windowId = windowId;
        return p;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onCloseScreen(this);
    }

    public override void Read(Stream stream)
    {
        windowId = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)windowId);
    }

    public override int Size()
    {
        return 1;
    }
}
