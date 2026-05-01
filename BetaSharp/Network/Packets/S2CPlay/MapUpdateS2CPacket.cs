namespace BetaSharp.Network.Packets.S2CPlay;

public class MapUpdateS2CPacket() : Packet(PacketId.MapUpdateS2C)
{
    public short MapId { get; private set; }
    public short ItemRawId { get; private set; }
    public byte[] UpdateData { get; private set; } = [];

    public static MapUpdateS2CPacket Get(short itemRawId, short id, byte[] updateData)
    {
        MapUpdateS2CPacket p = Get<MapUpdateS2CPacket>(PacketId.MapUpdateS2C);
        p.ItemRawId = itemRawId;
        p.MapId = id;
        p.UpdateData = updateData;
        return p;
    }

    public override void Read(Stream stream)
    {
        ItemRawId = stream.ReadShort();
        MapId = stream.ReadShort();
        UpdateData = new byte[(sbyte)stream.ReadByte() & 255];
        stream.ReadExactly(UpdateData);
    }

    public override void Write(Stream stream)
    {
        stream.WriteShort(ItemRawId);
        stream.WriteShort(MapId);
        stream.WriteByte((byte)UpdateData.Length);
        stream.Write(UpdateData);
    }

    public override void Apply(NetHandler handler) => handler.onMapUpdate(this);

    public override int Size() => 4 + UpdateData.Length;
}
