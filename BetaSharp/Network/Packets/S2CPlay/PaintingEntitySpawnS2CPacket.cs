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

    public PaintingEntitySpawnS2CPacket(EntityPainting paint) : this()
    {
        entityId = paint.id;
        xPosition = paint.XPosition;
        yPosition = paint.YPosition;
        zPosition = paint.ZPosition;
        direction = paint.Direction;
        title = paint.Art.Title;
    }

    public override void Read(NetworkStream stream)
    {
        entityId = stream.ReadInt();
        title = stream.ReadLongString((ushort) EnumArt.MaxArtTitleLength);
        xPosition = stream.ReadInt();
        yPosition = stream.ReadInt();
        zPosition = stream.ReadInt();
        direction = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
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
