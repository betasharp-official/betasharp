using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

internal class EntityRotateAndMoveRelativeS2CPacket : EntityS2CPacket
{
    public EntityRotateAndMoveRelativeS2CPacket() : base(PacketId.EntityRotateAndMoveRelativeS2C)
    {
        rotate = true;
    }

    public EntityRotateAndMoveRelativeS2CPacket(int entityId, byte deltaX, byte deltaY, byte deltaZ, byte yaw, byte pitch) : this()
    {
        EntityId = entityId;
        this.deltaX = (sbyte)deltaX;
        this.deltaY = (sbyte)deltaY;
        this.deltaZ = (sbyte)deltaZ;
        this.yaw = (sbyte)yaw;
        this.pitch = (sbyte)pitch;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        deltaX = (sbyte)stream.ReadByte();
        deltaY = (sbyte)stream.ReadByte();
        deltaZ = (sbyte)stream.ReadByte();
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
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
