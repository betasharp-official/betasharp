namespace BetaSharp.Network.Packets.S2CPlay;

internal interface IEntityRotatePacket : IPacketEntity
{
    sbyte Yaw { get; }
    sbyte Pitch { get; }
}

public class EntityRotateS2CPacket() : Packet(PacketId.EntityRotateS2C), IEntityRotatePacket
{
    public int EntityId { get; private set; }
    public sbyte Yaw { get; private set; }
    public sbyte Pitch { get; private set; }

    public static EntityRotateS2CPacket Get(int entityId, byte yaw, byte pitch)
    {
        EntityRotateS2CPacket p = Get<EntityRotateS2CPacket>(PacketId.EntityRotateS2C);
        p.EntityId = entityId;
        p.Yaw = (sbyte)yaw;
        p.Pitch = (sbyte)pitch;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Yaw = (sbyte)stream.ReadByte();
        Pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)Yaw);
        stream.WriteByte((byte)Pitch);
    }

    public override void Apply(NetHandler handler) => handler.onEntity(this);

    public override int Size() => 6;
}
