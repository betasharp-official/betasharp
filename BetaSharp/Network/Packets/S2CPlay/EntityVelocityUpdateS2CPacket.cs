using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityVelocityUpdateS2CPacket() : Packet(PacketId.EntityVelocityUpdateS2C), IPacketEntity
{
    public int MotionX { get; private set; }
    public int MotionY { get; private set; }
    public int MotionZ { get; private set; }
    public int EntityId { get; private set; }

    public static EntityVelocityUpdateS2CPacket Get(Entity ent) =>
        Get(ent.ID, ent.VelocityX, ent.VelocityY, ent.VelocityZ);

    public static EntityVelocityUpdateS2CPacket Get(int entityId, double motionX, double motionY, double motionZ)
    {
        EntityVelocityUpdateS2CPacket p = Get<EntityVelocityUpdateS2CPacket>(PacketId.EntityVelocityUpdateS2C);
        p.EntityId = entityId;
        double maxvelocity = 3.9D;
        if (motionX < -maxvelocity)
        {
            motionX = -maxvelocity;
        }

        if (motionY < -maxvelocity)
        {
            motionY = -maxvelocity;
        }

        if (motionZ < -maxvelocity)
        {
            motionZ = -maxvelocity;
        }

        if (motionX > maxvelocity)
        {
            motionX = maxvelocity;
        }

        if (motionY > maxvelocity)
        {
            motionY = maxvelocity;
        }

        if (motionZ > maxvelocity)
        {
            motionZ = maxvelocity;
        }

        p.MotionX = (int)(motionX * 8000.0D);
        p.MotionY = (int)(motionY * 8000.0D);
        p.MotionZ = (int)(motionZ * 8000.0D);
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        MotionX = stream.ReadShort();
        MotionY = stream.ReadShort();
        MotionZ = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteShort((short)MotionX);
        stream.WriteShort((short)MotionY);
        stream.WriteShort((short)MotionZ);
    }

    public override void Apply(NetHandler handler) => handler.onEntityVelocityUpdate(this);

    public override int Size() => 6 + IPacketEntity.PacketBaseEntitySize;
}
