using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ScreenHandlerPropertyUpdateS2CPacket() : Packet(PacketId.ScreenHandlerPropertyUpdateS2C)
{
    public int syncId;
    public int propertyId;
    public int value;

    public static ScreenHandlerPropertyUpdateS2CPacket Get(int syncId, int propertyId, int value)
    {
        var p = Get<ScreenHandlerPropertyUpdateS2CPacket>(PacketId.ScreenHandlerPropertyUpdateS2C);
        p.syncId = syncId;
        p.propertyId = propertyId;
        p.value = value;
        return p;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onScreenHandlerPropertyUpdate(this);
    }

    public override void Read(Stream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        propertyId = stream.ReadShort();
        value = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)syncId);
        stream.WriteShort((short)propertyId);
        stream.WriteShort((short)value);
    }

    public override int Size()
    {
        return 5;
    }
}
