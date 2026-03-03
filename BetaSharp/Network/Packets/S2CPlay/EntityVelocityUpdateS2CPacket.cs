using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityVelocityUpdateS2CPacket() : PacketBaseEntity(PacketId.EntityVelocityUpdateS2C)
{
    public int motionX;
    public int motionY;
    public int motionZ;

    public EntityVelocityUpdateS2CPacket(Entity ent) : this(ent.id, ent.velocityX, ent.velocityY, ent.velocityZ) { }

    public EntityVelocityUpdateS2CPacket(int entityId, double motionX, double motionY, double motionZ) : this()
    {
        EntityId = entityId;
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

        this.motionX = (int)(motionX * 8000.0D);
        this.motionY = (int)(motionY * 8000.0D);
        this.motionZ = (int)(motionZ * 8000.0D);
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
