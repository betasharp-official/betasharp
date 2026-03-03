namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityS2CPacket(PacketId id = PacketId.EntityS2C) : PacketBaseEntity(id)
{
    public sbyte deltaX;
    public sbyte deltaY;
    public sbyte deltaZ;
    public sbyte yaw;
    public sbyte pitch;
    public bool rotate = false;

    public EntityS2CPacket(int entityId) : this()
    {
        EntityId = entityId;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntity(this);
    }
}
