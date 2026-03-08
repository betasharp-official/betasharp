using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityVelocityUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityVelocityUpdateS2C)
{
    public int motionX;
    public int motionY;
    public int motionZ;

    public static EntityVelocityUpdateS2CPacket Get(Entity ent) =>
        Get(ent.id, ent.velocityX, ent.velocityY, ent.velocityZ);

    public static EntityVelocityUpdateS2CPacket Get(int entityId, double motionX, double motionY, double motionZ)
    {
        var p = Get<EntityVelocityUpdateS2CPacket>(PacketId.EntityVelocityUpdateS2C);
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

        p.motionX = (int)(motionX * 8000.0D);
        p.motionY = (int)(motionY * 8000.0D);
        p.motionZ = (int)(motionZ * 8000.0D);
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        motionX = stream.ReadShort();
        motionY = stream.ReadShort();
        motionZ = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        stream.WriteShort((short)motionX);
        stream.WriteShort((short)motionY);
        stream.WriteShort((short)motionZ);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityVelocityUpdate(this);
    }

    public override int Size()
    {
        return 6 + PacketBaseEntitySize;
    }
}
