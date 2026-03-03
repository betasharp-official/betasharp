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

    public EntityPositionS2CPacket(int entityId, int x, int y, int z, byte yaw, byte pitch) : this()
    {
        EntityId = entityId;
        this.x = x;
        this.y = y;
        this.z = z;
        this.yaw = (sbyte)yaw;
        this.pitch = (sbyte)pitch;
    }

    public EntityPositionS2CPacket(Entity entity) : this()
    {
        EntityId = entity.id;
        x = MathHelper.Floor(entity.x * 32.0D);
        y = MathHelper.Floor(entity.y * 32.0D);
        z = MathHelper.Floor(entity.z * 32.0D);
        yaw = (sbyte)(int)(entity.yaw * 256.0F / 360.0F);
        pitch = (sbyte)(int)(entity.pitch * 256.0F / 360.0F);
    }

    public override void Read(NetworkStream stream)
    {
        base.Read(stream);
        x = stream.ReadInt();
        y = stream.ReadInt();
        z = stream.ReadInt();
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
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
