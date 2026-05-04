namespace BetaSharp.Network.Packets.S2CPlay;

public class ScreenHandlerPropertyUpdateS2CPacket() : Packet(PacketId.ScreenHandlerPropertyUpdateS2C)
{
    public int PropertyId { get; private set; }
    public int SyncId { get; private set; }
    public int Value { get; private set; }

    public static ScreenHandlerPropertyUpdateS2CPacket Get(int syncId, int propertyId, int value)
    {
        ScreenHandlerPropertyUpdateS2CPacket p = Get<ScreenHandlerPropertyUpdateS2CPacket>(PacketId.ScreenHandlerPropertyUpdateS2C);
        p.SyncId = syncId;
        p.PropertyId = propertyId;
        p.Value = value;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onScreenHandlerPropertyUpdate(this);

    public override void Read(Stream stream)
    {
        SyncId = (sbyte)stream.ReadByte();
        PropertyId = stream.ReadShort();
        Value = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)SyncId);
        stream.WriteShort((short)PropertyId);
        stream.WriteShort((short)Value);
    }

    public override int Size() => 5;
}
