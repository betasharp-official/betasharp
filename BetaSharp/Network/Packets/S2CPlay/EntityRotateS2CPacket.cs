using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

internal class EntityRotateS2CPacket : EntityS2CPacket
{
    public EntityRotateS2CPacket() : base(PacketId.EntityRotateS2C)
    {
        rotate = true;
    }

    public static EntityRotateS2CPacket Get(int entityId, byte yaw, byte pitch)
    {
        var p = Get<EntityRotateS2CPacket>(PacketId.EntityRotateS2C);
        p.EntityId = entityId;
        p.deltaX = 0;
        p.deltaY = 0;
        p.deltaZ = 0;
        p.yaw = (sbyte)yaw;
        p.pitch = (sbyte)pitch;
        p.rotate = true;
        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        stream.WriteByte((byte)yaw);
        stream.WriteByte((byte)pitch);
    }

    public override int Size()
    {
        return 6;
    }
}
