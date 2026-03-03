using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ScreenHandlerPropertyUpdateS2CPacket() : Packet(PacketId.ScreenHandlerPropertyUpdateS2C)
{
    public int syncId;
    public int propertyId;
    public int value;

    public ScreenHandlerPropertyUpdateS2CPacket(int syncId, int propertyId, int value) : this()
    {
        this.syncId = syncId;
        this.propertyId = propertyId;
        this.value = value;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onScreenHandlerPropertyUpdate(this);
    }

    public override void Read(NetworkStream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        propertyId = stream.ReadShort();
        value = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
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
