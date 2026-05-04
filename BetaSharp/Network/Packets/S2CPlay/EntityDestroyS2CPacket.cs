namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityDestroyS2CPacket() : Packet(PacketId.EntityDestroyS2C), IPacketEntity
{
    public int EntityId { get; private set; }

    public static EntityDestroyS2CPacket Get(int entityId)
    {
        EntityDestroyS2CPacket p = Get<EntityDestroyS2CPacket>(PacketId.EntityDestroyS2C);
        p.EntityId = entityId;
        return p;
    }

    public override void Read(Stream stream) => EntityId = stream.ReadInt();

    public override void Write(Stream stream) => stream.WriteInt(EntityId);

    public override int Size() => IPacketEntity.PacketBaseEntitySize;

    public override void Apply(NetHandler handler) => handler.onEntityDestroy(this);
}
