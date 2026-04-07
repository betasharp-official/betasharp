using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

internal class EntityRotateAndMoveRelativeS2CPacket : EntityS2CPacket
{
    public EntityRotateAndMoveRelativeS2CPacket() : base(PacketId.EntityRotateAndMoveRelativeS2C)
    {
        rotate = true;
    }

    public static EntityRotateAndMoveRelativeS2CPacket Get(int entityId, byte deltaX, byte deltaY, byte deltaZ, byte yaw, byte pitch)
    {
        var p = Get<EntityRotateAndMoveRelativeS2CPacket>(PacketId.EntityRotateAndMoveRelativeS2C);
        p.EntityId = entityId;
        p.deltaX = (sbyte)deltaX;
        p.deltaY = (sbyte)deltaY;
        p.deltaZ = (sbyte)deltaZ;
        p.yaw = (sbyte)yaw;
        p.pitch = (sbyte)pitch;
        p.rotate = true;
        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        deltaX = (sbyte)stream.ReadByte();
        deltaY = (sbyte)stream.ReadByte();
        deltaZ = (sbyte)stream.ReadByte();
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        stream.WriteByte((byte)deltaX);
        stream.WriteByte((byte)deltaY);
        stream.WriteByte((byte)deltaZ);
        stream.WriteByte((byte)yaw);
        stream.WriteByte((byte)pitch);
    }

    public override int Size()
    {
        return 9;
    }
}
