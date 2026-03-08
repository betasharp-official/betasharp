using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntitySpawnS2CPacket() : PacketBaseEntity(PacketId.EntitySpawnS2C)
{
    public int x;
    public int y;
    public int z;
    public int velocityX;
    public int velocityY;
    public int velocityZ;
    public int entityType;
    public int entityData;

    public static EntitySpawnS2CPacket Get(Entity entity, int entityType, int entityData = 0)
    {
        var p = Get<EntitySpawnS2CPacket>(PacketId.EntitySpawnS2C);
        p.EntityId = entity.id;
        p.x = MathHelper.Floor(entity.x * 32.0);
        p.y = MathHelper.Floor(entity.y * 32.0);
        p.z = MathHelper.Floor(entity.z * 32.0);
        p.entityType = entityType;
        p.entityData = entityData;
        p.velocityX = 0;
        p.velocityY = 0;
        p.velocityZ = 0;

        if (entityData > 0)
        {
            double velocityX = entity.velocityX;
            double velocityY = entity.velocityY;
            double velocityZ = entity.velocityZ;
            double maxVelocity = 3.9;
            if (velocityX < -maxVelocity)
            {
                velocityX = -maxVelocity;
            }

            if (velocityY < -maxVelocity)
            {
                velocityY = -maxVelocity;
            }

            if (velocityZ < -maxVelocity)
            {
                velocityZ = -maxVelocity;
            }

            if (velocityX > maxVelocity)
            {
                velocityX = maxVelocity;
            }

            if (velocityY > maxVelocity)
            {
                velocityY = maxVelocity;
            }

            if (velocityZ > maxVelocity)
            {
                velocityZ = maxVelocity;
            }

            p.velocityX = (int)(velocityX * 8000.0);
            p.velocityY = (int)(velocityY * 8000.0);
            p.velocityZ = (int)(velocityZ * 8000.0);
        }

        return p;
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        entityType = (sbyte)stream.ReadByte();
        x = stream.ReadInt();
        y = stream.ReadInt();
        z = stream.ReadInt();
        entityData = stream.ReadInt();
        if (entityData > 0)
        {
            velocityX = stream.ReadShort();
            velocityY = stream.ReadShort();
            velocityZ = stream.ReadShort();
        }

    }

    public override void Write(NetworkStream stream)
    {
        base.Write(stream);
        stream.WriteByte((byte)entityType);
        stream.WriteInt(x);
        stream.WriteInt(y);
        stream.WriteInt(z);
        stream.WriteInt(entityData);
        if (entityData > 0)
        {
            stream.WriteShort((short)velocityX);
            stream.WriteShort((short)velocityY);
            stream.WriteShort((short)velocityZ);
        }

    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntitySpawn(this);
    }

    public override int Size()
    {
        return 17 + PacketBaseEntitySize + entityData > 0 ? 6 : 0;
    }
}
