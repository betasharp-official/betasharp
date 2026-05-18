namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityS2CPacket(PacketId id = PacketId.EntityS2C) : Packet(id), IPacketEntity
{
    public int EntityId { get; private set; }

    public static EntityS2CPacket Get(int entityId)
    {
        EntityS2CPacket p = Get<EntityS2CPacket>(PacketId.EntityS2C);
        p.EntityId = entityId;
        return p;
    }

    public override void Read(Stream stream) => EntityId = stream.ReadInt();

    public override void Write(Stream stream) => stream.WriteInt(EntityId);

    public override int Size() => IPacketEntity.PacketBaseEntitySize;

    public override void Apply(NetHandler handler) => handler.onEntity(this);
}
