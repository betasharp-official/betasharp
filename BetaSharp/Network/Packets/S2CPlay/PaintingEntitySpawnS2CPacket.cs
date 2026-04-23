using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PaintingEntitySpawnS2CPacket() : Packet(PacketId.PaintingEntitySpawnS2C)
{
    public int entityId;
    public int xPosition;
    public int yPosition;
    public int zPosition;
    public int direction;
    public string title;

    public static PaintingEntitySpawnS2CPacket Get(EntityPainting paint)
    {
        var p = Get<PaintingEntitySpawnS2CPacket>(PacketId.PaintingEntitySpawnS2C);
        p.entityId = paint.ID;
        p.xPosition = paint.XPosition;
        p.yPosition = paint.YPosition;
        p.zPosition = paint.ZPosition;
        p.direction = paint.Direction;
        p.title = paint.Art.Title;
        return p;
    }

    public override void Read(Stream stream)
    {
        entityId = stream.ReadInt();
        title = stream.ReadLongString((ushort)Painting.MaxArtTitleLength);
        xPosition = stream.ReadInt();
        yPosition = stream.ReadInt();
        zPosition = stream.ReadInt();
        direction = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteLongString(title);
        stream.WriteInt(xPosition);
        stream.WriteInt(yPosition);
        stream.WriteInt(zPosition);
        stream.WriteInt(direction);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPaintingEntitySpawn(this);
    }

    public override int Size()
    {
        return 24;
    }
}
