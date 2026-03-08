using System.Net.Sockets;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;

namespace BetaSharp.Network.Packets.S2CPlay;

public class GlobalEntitySpawnS2CPacket() : Packet(PacketId.GlobalEntitySpawnS2C)
{
    public int id;
    public int x;
    public int y;
    public int z;
    public byte type;

    public static GlobalEntitySpawnS2CPacket Get(Entity ent)
    {
        var p = Get<GlobalEntitySpawnS2CPacket>(PacketId.GlobalEntitySpawnS2C);
        p.id = ent.id;
        p.x = MathHelper.Floor(ent.x * 32.0D);
        p.y = MathHelper.Floor(ent.y * 32.0D);
        p.z = MathHelper.Floor(ent.z * 32.0D);
        if (ent is EntityLightningBolt)
        {
            p.type = 1;
        }

        return p;
    }

    public override void Read(NetworkStream stream)
    {
        id = stream.ReadInt();
        type = (byte)stream.ReadByte();
        x = stream.ReadInt();
        y = stream.ReadInt();
        z = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(id);
        stream.WriteByte(type);
        stream.WriteInt(x);
        stream.WriteInt(y);
        stream.WriteInt(z);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onLightningEntitySpawn(this);
    }

    public override int Size()
    {
        return 17;
    }
}
