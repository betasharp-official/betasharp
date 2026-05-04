using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityPositionS2CPacket() : Packet(PacketId.EntityPositionS2C), IPacketEntity
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }
    public sbyte Yaw { get; private set; }
    public sbyte Pitch { get; private set; }

    public int EntityId { get; private set; }

    public static EntityPositionS2CPacket Get(int entityId, int x, int y, int z, byte yaw, byte pitch)
    {
        EntityPositionS2CPacket p = Get<EntityPositionS2CPacket>(PacketId.EntityPositionS2C);
        p.EntityId = entityId;
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.Yaw = (sbyte)yaw;
        p.Pitch = (sbyte)pitch;
        return p;
    }

    public static EntityPositionS2CPacket Get(Entity entity)
    {
        EntityPositionS2CPacket p = Get<EntityPositionS2CPacket>(PacketId.EntityPositionS2C);
        p.EntityId = entity.ID;
        p.X = MathHelper.Floor(entity.X * 32.0D);
        p.Y = MathHelper.Floor(entity.Y * 32.0D);
        p.Z = MathHelper.Floor(entity.Z * 32.0D);
        p.Yaw = (sbyte)(int)(entity.Yaw * 256.0F / 360.0F);
        p.Pitch = (sbyte)(int)(entity.Pitch * 256.0F / 360.0F);
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        X = stream.ReadInt();
        Y = stream.ReadInt();
        Z = stream.ReadInt();
        Yaw = (sbyte)stream.ReadByte();
        Pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteInt(X);
        stream.WriteInt(Y);
        stream.WriteInt(Z);
        stream.WriteByte((byte)Yaw);
        stream.WriteByte((byte)Pitch);
    }

    public override void Apply(NetHandler handler) => handler.onEntityPosition(this);

    public override int Size() => 30 + IPacketEntity.PacketBaseEntitySize;
}
