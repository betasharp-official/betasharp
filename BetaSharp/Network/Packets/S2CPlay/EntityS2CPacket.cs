namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityS2CPacket(PacketId id = PacketId.EntityS2C) : Packet(id), IPacketEntity
{
    public sbyte DeltaX { get; private set; }
    public sbyte DeltaY { get; private set; }
    public sbyte DeltaZ { get; private set; }
    public sbyte Yaw { get; private set; }
    public sbyte Pitch { get; private set; }
    public bool Rotate { get; private set; }
    public int EntityId { get; private set; }

    public static EntityS2CPacket Get(int entityId)
    {
        EntityS2CPacket p = Get<EntityS2CPacket>(PacketId.EntityS2C);
        p.EntityId = entityId;
        p.DeltaX = 0;
        p.DeltaY = 0;
        p.DeltaZ = 0;
        p.Yaw = 0;
        p.Pitch = 0;
        p.Rotate = false;
        return p;
    }

    public override void Read(Stream stream) => EntityId = stream.ReadInt();

    public override void Write(Stream stream) => stream.WriteInt(EntityId);

    public override int Size() => IPacketEntity.PacketBaseEntitySize;

    public override void Apply(NetHandler handler) => handler.onEntity(this);
}
