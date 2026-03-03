using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class MapUpdateS2CPacket() : Packet(PacketId.MapUpdateS2C)
{
    public short itemRawId;
    public short id;
    public byte[] updateData;

    public MapUpdateS2CPacket(short itemRawId, short id, byte[] updateData) : this()
    {
        this.itemRawId = itemRawId;
        this.id = id;
        this.updateData = updateData;
    }

    public override void Read(NetworkStream stream)
    {
        itemRawId = stream.ReadShort();
        id = stream.ReadShort();
        updateData = new byte[(sbyte)stream.ReadByte() & 255];
        stream.ReadExactly(updateData);
    }

    public override void Write(NetworkStream stream)
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
