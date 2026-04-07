using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ItemPickupAnimationS2CPacket() : Packet(PacketId.ItemPickupAnimationS2C)
{
    public int entityId;
    public int collectorEntityId;

    public static ItemPickupAnimationS2CPacket Get(int entityId, int collectorId)
    {
        var p = Get<ItemPickupAnimationS2CPacket>(PacketId.ItemPickupAnimationS2C);
        p.entityId = entityId;
        p.collectorEntityId = collectorId;
        return p;
    }

    public override void Read(Stream stream)
    {
        entityId = stream.ReadInt();
        collectorEntityId = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteInt(collectorEntityId);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onItemPickupAnimation(this);
    }

    public override int Size()
    {
        return 8;
    }
}
