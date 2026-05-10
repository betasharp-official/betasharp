namespace BetaSharp.Network.Packets.S2CPlay;

public class ItemPickupAnimationS2CPacket() : Packet(PacketId.ItemPickupAnimationS2C), IPacketEntity
{
    public int CollectorEntityId { get; private set; }
    public int EntityId { get; private set; }

    public static ItemPickupAnimationS2CPacket Get(int entityId, int collectorId)
    {
        ItemPickupAnimationS2CPacket p = Get<ItemPickupAnimationS2CPacket>(PacketId.ItemPickupAnimationS2C);
        p.EntityId = entityId;
        p.CollectorEntityId = collectorId;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        CollectorEntityId = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteInt(CollectorEntityId);
    }

    public override void Apply(NetHandler handler) => handler.onItemPickupAnimation(this);

    public override int Size() => 8;
}
