using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class EntityPositionS2CPacket() : PacketBaseEntity(PacketId.EntityPositionS2C)
{
    public int x;
    public int y;
    public int z;
    public sbyte yaw;
    public sbyte pitch;

    public static EntityPositionS2CPacket Get(int entityId, int x, int y, int z, byte yaw, byte pitch)
    {
        var p = Get<EntityPositionS2CPacket>(PacketId.EntityPositionS2C);
        p.EntityId = entityId;
        p.x = x;
        p.y = y;
        p.z = z;
        p.yaw = (sbyte)yaw;
        p.pitch = (sbyte)pitch;
        return p;
    }

    public static EntityPositionS2CPacket Get(Entity entity)
    {
        var p = Get<EntityPositionS2CPacket>(PacketId.EntityPositionS2C);
        p.EntityId = entity.ID;
        p.x = MathHelper.Floor(entity.X * 32.0D);
        p.y = MathHelper.Floor(entity.Y * 32.0D);
        p.z = MathHelper.Floor(entity.Z * 32.0D);
        p.yaw = (sbyte)(int)(entity.Yaw * 256.0F / 360.0F);
        p.pitch = (sbyte)(int)(entity.Pitch * 256.0F / 360.0F);
        return p;
    }

    public override void Read(Stream stream)
    {
        base.Read(stream);
        x = stream.ReadInt();
        y = stream.ReadInt();
        z = stream.ReadInt();
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        base.Write(stream);
        stream.WriteInt(x);
        stream.WriteInt(y);
        stream.WriteInt(z);
        stream.WriteByte((byte)yaw);
        stream.WriteByte((byte)pitch);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onEntityPosition(this);
    }

    public override int Size()
    {
        return 30 + PacketBaseEntitySize;
    }
}
