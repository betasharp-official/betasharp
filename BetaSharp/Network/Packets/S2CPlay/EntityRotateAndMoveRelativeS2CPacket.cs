namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityRotateAndMoveRelativeS2CPacket() : Packet(PacketId.EntityRotateAndMoveRelativeS2C), IEntityMoveRelativePacket, IEntityRotatePacket
{
    public int EntityId { get; private set; }
    public sbyte DeltaX { get; private set; }
    public sbyte DeltaY { get; private set; }
    public sbyte DeltaZ { get; private set; }
    public sbyte Yaw { get; private set; }
    public sbyte Pitch { get; private set; }

    public static EntityRotateAndMoveRelativeS2CPacket Get(int entityId, byte deltaX, byte deltaY, byte deltaZ, byte yaw, byte pitch)
    {
        EntityRotateAndMoveRelativeS2CPacket p = Get<EntityRotateAndMoveRelativeS2CPacket>(PacketId.EntityRotateAndMoveRelativeS2C);
        p.EntityId = entityId;
        p.DeltaX = (sbyte)deltaX;
        p.DeltaY = (sbyte)deltaY;
        p.DeltaZ = (sbyte)deltaZ;
        p.Yaw = (sbyte)yaw;
        p.Pitch = (sbyte)pitch;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        DeltaX = (sbyte)stream.ReadByte();
        DeltaY = (sbyte)stream.ReadByte();
        DeltaZ = (sbyte)stream.ReadByte();
        Yaw = (sbyte)stream.ReadByte();
        Pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)DeltaX);
        stream.WriteByte((byte)DeltaY);
        stream.WriteByte((byte)DeltaZ);
        stream.WriteByte((byte)Yaw);
        stream.WriteByte((byte)Pitch);
    }

    public override void Apply(NetHandler handler) => handler.onEntity(this);

    public override int Size() => 9;
}
