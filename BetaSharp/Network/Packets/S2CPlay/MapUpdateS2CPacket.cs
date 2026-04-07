using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class MapUpdateS2CPacket() : Packet(PacketId.MapUpdateS2C)
{
    public short itemRawId;
    public short id;
    public byte[] updateData;

    public static MapUpdateS2CPacket Get(short itemRawId, short id, byte[] updateData)
    {
        var p = Get<MapUpdateS2CPacket>(PacketId.MapUpdateS2C);
        p.itemRawId = itemRawId;
        p.id = id;
        p.updateData = updateData;
        return p;
    }

    public override void Read(Stream stream)
    {
        itemRawId = stream.ReadShort();
        id = stream.ReadShort();
        updateData = new byte[(sbyte)stream.ReadByte() & 255];
        stream.ReadExactly(updateData);
    }

    public override void Write(Stream stream)
    {
        stream.WriteShort(itemRawId);
        stream.WriteShort(id);
        stream.WriteByte((byte)updateData.Length);
        stream.Write(updateData);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onMapUpdate(this);
    }

    public override int Size()
    {
        return 4 + updateData.Length;
    }
}
