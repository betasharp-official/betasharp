using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

internal class EntityMoveRelativeS2CPacket() : EntityS2CPacket(PacketId.EntityMoveRelativeS2C)
{

    public static EntityMoveRelativeS2CPacket Get(int entityId, byte deltaX, byte deltaY, byte deltaZ)
    {
        var p = Get<EntityMoveRelativeS2CPacket>(PacketId.EntityMoveRelativeS2C);
        p.EntityId = entityId;
        p.deltaX = (sbyte)deltaX;
        p.deltaY = (sbyte)deltaY;
        p.deltaZ = (sbyte)deltaZ;
        p.yaw = 0;
        p.pitch = 0;
        p.rotate = false;
        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        deltaX = (sbyte)stream.ReadByte();
        deltaY = (sbyte)stream.ReadByte();
        deltaZ = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        stream.WriteByte((byte)deltaX);
        stream.WriteByte((byte)deltaY);
        stream.WriteByte((byte)deltaZ);
    }

    public override int Size()
    {
        return 7;
    }
}
