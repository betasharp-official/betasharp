using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class LivingEntitySpawnS2CPacket() : Packet(PacketId.LivingEntitySpawnS2C), IPacketEntity
{
    public byte[] Data { get; private set; } = [];
    public sbyte Pitch { get; private set; }
    public sbyte Type { get; private set; }
    public int XPosition { get; private set; }
    public int YPosition { get; private set; }
    public int ZPosition { get; private set; }
    public sbyte Yaw { get; private set; }
    public int EntityId { get; private set; }

    public static LivingEntitySpawnS2CPacket Get(EntityLiving ent)
    {
        LivingEntitySpawnS2CPacket p = Get<LivingEntitySpawnS2CPacket>(PacketId.LivingEntitySpawnS2C);
        p.EntityId = ent.ID;
        p.Type = (sbyte)EntityRegistry.GetRawId(ent);
        p.XPosition = MathHelper.Floor(ent.X * 32.0D);
        p.YPosition = MathHelper.Floor(ent.Y * 32.0D);
        p.ZPosition = MathHelper.Floor(ent.Z * 32.0D);
        p.Yaw = (sbyte)(int)(ent.Yaw * 256.0F / 360.0F);
        p.Pitch = (sbyte)(int)(ent.Pitch * 256.0F / 360.0F);
        MemoryStream stream = new();
        ent.DataSynchronizer.WriteAll(stream);
        p.Data = stream.ToArray();
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Type = (sbyte)stream.ReadByte();
        XPosition = stream.ReadInt();
        YPosition = stream.ReadInt();
        ZPosition = stream.ReadInt();
        Yaw = (sbyte)stream.ReadByte();
        Pitch = (sbyte)stream.ReadByte();
        Data = stream.ReadUntil(127);
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)Type);
        stream.WriteInt(XPosition);
        stream.WriteInt(YPosition);
        stream.WriteInt(ZPosition);
        stream.WriteByte((byte)Yaw);
        stream.WriteByte((byte)Pitch);
        stream.Write(Data);
        stream.WriteByte(127);
    }

    public override void Apply(NetHandler handler) => handler.onLivingEntitySpawn(this);

    public override int Size() => 20;
}
