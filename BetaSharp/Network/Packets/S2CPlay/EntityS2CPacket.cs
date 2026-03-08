namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityS2CPacket(PacketId id = PacketId.EntityS2C) : PacketBaseEntity(id)
{
    public sbyte deltaX;
    public sbyte deltaY;
    public sbyte deltaZ;
    public sbyte yaw;
    public sbyte pitch;
    public bool rotate = false;

    public static EntityS2CPacket Get(int entityId)
    {
        var p = Get<EntityS2CPacket>(PacketId.EntityS2C);
        p.EntityId = entityId;
        p.deltaX = 0;
        p.deltaY = 0;
        p.deltaZ = 0;
        p.yaw = 0;
        p.pitch = 0;
        p.rotate = false;
        return p;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntity(this);
    }
}
