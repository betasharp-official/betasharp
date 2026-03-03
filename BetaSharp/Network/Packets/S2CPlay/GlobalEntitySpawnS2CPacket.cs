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

    public GlobalEntitySpawnS2CPacket(Entity ent) : this()
    {
        id = ent.id;
        x = MathHelper.Floor(ent.x * 32.0D);
        y = MathHelper.Floor(ent.y * 32.0D);
        z = MathHelper.Floor(ent.z * 32.0D);
        if (ent is EntityLightningBolt)
        {
            type = 1;
        }

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
