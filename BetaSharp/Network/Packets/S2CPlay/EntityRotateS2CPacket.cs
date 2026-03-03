using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

internal class EntityRotateS2CPacket : EntityS2CPacket
{
    public EntityRotateS2CPacket() : base(PacketId.EntityRotateS2C)
    {
        rotate = true;
    }

    public EntityRotateS2CPacket(int entityId, byte yaw, byte pitch) : this()
    {
        EntityId = entityId;
        this.yaw = (sbyte)yaw;
        this.pitch = (sbyte)pitch;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
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
