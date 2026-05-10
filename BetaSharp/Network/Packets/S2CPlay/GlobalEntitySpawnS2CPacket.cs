using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class GlobalEntitySpawnS2CPacket() : Packet(PacketId.GlobalEntitySpawnS2C), IPacketEntity
{
    public byte Type { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }
    public int EntityId { get; private set; }

    public static GlobalEntitySpawnS2CPacket Get(Entity ent)
    {
        GlobalEntitySpawnS2CPacket p = Get<GlobalEntitySpawnS2CPacket>(PacketId.GlobalEntitySpawnS2C);
        p.EntityId = ent.ID;
        p.X = MathHelper.Floor(ent.X * 32.0D);
        p.Y = MathHelper.Floor(ent.Y * 32.0D);
        p.Z = MathHelper.Floor(ent.Z * 32.0D);
        if (ent is EntityLightningBolt)
        {
            p.Type = 1;
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Type = (byte)stream.ReadByte();
        X = stream.ReadInt();
        Y = stream.ReadInt();
        Z = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte(Type);
        stream.WriteInt(X);
        stream.WriteInt(Y);
        stream.WriteInt(Z);
    }

    public override void Apply(NetHandler handler) => handler.onLightningEntitySpawn(this);

    public override int Size() => 17;
}
