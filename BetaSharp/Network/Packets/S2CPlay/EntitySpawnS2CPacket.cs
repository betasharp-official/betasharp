using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntitySpawnS2CPacket() : Packet(PacketId.EntitySpawnS2C), IPacketEntity
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }
    public int VelocityX { get; set; }
    public int VelocityY { get; set; }
    public int VelocityZ { get; set; }
    public int EntityType { get; private set; }
    public int EntityData { get; set; }
    public int EntityId { get; private set; }

    public static EntitySpawnS2CPacket Get(Entity entity, int entityType, int entityData = 0)
    {
        EntitySpawnS2CPacket p = Get<EntitySpawnS2CPacket>(PacketId.EntitySpawnS2C);
        p.EntityId = entity.ID;
        p.X = MathHelper.Floor(entity.X * 32.0);
        p.Y = MathHelper.Floor(entity.Y * 32.0);
        p.Z = MathHelper.Floor(entity.Z * 32.0);
        p.EntityType = entityType;
        p.EntityData = entityData;
        p.VelocityX = 0;
        p.VelocityY = 0;
        p.VelocityZ = 0;

        if (entityData > 0)
        {
            double velocityX = entity.VelocityX;
            double velocityY = entity.VelocityY;
            double velocityZ = entity.VelocityZ;
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

            p.VelocityX = (int)(velocityX * 8000.0);
            p.VelocityY = (int)(velocityY * 8000.0);
            p.VelocityZ = (int)(velocityZ * 8000.0);
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        EntityType = (sbyte)stream.ReadByte();
        X = stream.ReadInt();
        Y = stream.ReadInt();
        Z = stream.ReadInt();
        EntityData = stream.ReadInt();
        if (EntityData > 0)
        {
            VelocityX = stream.ReadShort();
            VelocityY = stream.ReadShort();
            VelocityZ = stream.ReadShort();
        }
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)EntityType);
        stream.WriteInt(X);
        stream.WriteInt(Y);
        stream.WriteInt(Z);
        stream.WriteInt(EntityData);
        if (EntityData > 0)
        {
            stream.WriteShort((short)VelocityX);
            stream.WriteShort((short)VelocityY);
            stream.WriteShort((short)VelocityZ);
        }
    }

    public override void Apply(NetHandler handler) => handler.onEntitySpawn(this);

    public override int Size() => 17 + IPacketEntity.PacketBaseEntitySize + EntityData > 0 ? 6 : 0;
}
