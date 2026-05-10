namespace BetaSharp.Network.Packets.S2CPlay;

internal interface IEntityMoveRelativePacket : IPacketEntity
{
    sbyte DeltaX { get; }
    sbyte DeltaY { get; }
    sbyte DeltaZ { get; }
}

public class EntityMoveRelativeS2CPacket() : Packet(PacketId.EntityMoveRelativeS2C), IEntityMoveRelativePacket
{
    public int EntityId { get; private set; }
    public sbyte DeltaX { get; private set; }
    public sbyte DeltaY { get; private set; }
    public sbyte DeltaZ { get; private set; }

    public static EntityMoveRelativeS2CPacket Get(int entityId, byte deltaX, byte deltaY, byte deltaZ)
    {
        EntityMoveRelativeS2CPacket p = Get<EntityMoveRelativeS2CPacket>(PacketId.EntityMoveRelativeS2C);
        p.EntityId = entityId;
        p.DeltaX = (sbyte)deltaX;
        p.DeltaY = (sbyte)deltaY;
        p.DeltaZ = (sbyte)deltaZ;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        DeltaX = (sbyte)stream.ReadByte();
        DeltaY = (sbyte)stream.ReadByte();
        DeltaZ = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)DeltaX);
        stream.WriteByte((byte)DeltaY);
        stream.WriteByte((byte)DeltaZ);
    }

    public override void Apply(NetHandler handler) => handler.onEntity(this);

    public override int Size() => 7;
}
