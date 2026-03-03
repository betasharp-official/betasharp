using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityStatusS2CPacket() : PacketBaseEntity(PacketId.EntityStatusS2C)
{
    public sbyte EntityStatus;

    public EntityStatusS2CPacket(int entityId, byte status) : this()
    {
        EntityId = entityId;
        EntityStatus = (sbyte)status;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        EntityStatus = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        stream.WriteByte((byte)EntityStatus);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityStatus(this);
    }

    public override int Size()
    {
        return PacketBaseEntitySize + 1;
    }
}
