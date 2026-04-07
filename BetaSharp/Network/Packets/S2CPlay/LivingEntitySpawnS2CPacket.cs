using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class LivingEntitySpawnS2CPacket() : Packet(PacketId.LivingEntitySpawnS2C)
{
    public int entityId;
    public sbyte type;
    public int xPosition;
    public int yPosition;
    public int zPosition;
    public sbyte yaw;
    public sbyte pitch;
    public byte[] Data;

    public static LivingEntitySpawnS2CPacket Get(EntityLiving ent)
    {
        var p = Get<LivingEntitySpawnS2CPacket>(PacketId.LivingEntitySpawnS2C);
        p.entityId = ent.id;
        p.type = (sbyte)EntityRegistry.GetRawId(ent);
        p.xPosition = MathHelper.Floor(ent.x * 32.0D);
        p.yPosition = MathHelper.Floor(ent.y * 32.0D);
        p.zPosition = MathHelper.Floor(ent.z * 32.0D);
        p.yaw = (sbyte)(int)(ent.yaw * 256.0F / 360.0F);
        p.pitch = (sbyte)(int)(ent.pitch * 256.0F / 360.0F);
        var stream = new MemoryStream();
        ent.DataSynchronizer.WriteAll(stream);
        p.Data = stream.ToArray();
        return p;
    }

    public override void Read(Stream stream)
    {
        entityId = stream.ReadInt();
        type = (sbyte)stream.ReadByte();
        xPosition = stream.ReadInt();
        yPosition = stream.ReadInt();
        zPosition = stream.ReadInt();
        yaw = (sbyte)stream.ReadByte();
        pitch = (sbyte)stream.ReadByte();
        Data = stream.ReadUntil(127);
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteByte((byte)type);
        stream.WriteInt(xPosition);
        stream.WriteInt(yPosition);
        stream.WriteInt(zPosition);
        stream.WriteByte((byte)yaw);
        stream.WriteByte((byte)pitch);
        stream.Write(Data);
        stream.WriteByte(127);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onLivingEntitySpawn(this);
    }

    public override int Size()
    {
        return 20;
    }
}
