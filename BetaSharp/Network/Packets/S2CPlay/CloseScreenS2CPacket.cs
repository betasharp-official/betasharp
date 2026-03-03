using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class CloseScreenS2CPacket() : Packet(PacketId.CloseScreenS2C)
{
    public int windowId;

    public CloseScreenS2CPacket(int windowId) : this()
    {
        this.windowId = windowId;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onCloseScreen(this);
    }

    public override void Read(NetworkStream stream)
    {
        windowId = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)windowId);
    }

    public override int Size()
    {
        return 1;
    }
}
