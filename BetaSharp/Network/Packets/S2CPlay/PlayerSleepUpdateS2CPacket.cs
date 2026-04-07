using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerSleepUpdateS2CPacket() : Packet(PacketId.PlayerSleepUpdateS2C)
{
    public int id;
    public int x;
    public int y;
    public int z;
    public int status;

    public static PlayerSleepUpdateS2CPacket Get(Entity player, int status, int x, int y, int z)
    {
        var p = Get<PlayerSleepUpdateS2CPacket>(PacketId.PlayerSleepUpdateS2C);
        p.status = status;
        p.x = x;
        p.y = y;
        p.z = z;
        p.id = player.id;
        return p;
    }

    public override void Read(Stream stream)
    {
        id = stream.ReadInt();
        status = (sbyte)stream.ReadByte();
        x = stream.ReadInt();
        y = (sbyte)stream.ReadByte();
        z = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(id);
        stream.WriteByte((byte)status);
        stream.WriteInt(x);
        stream.WriteByte((byte)y);
        stream.WriteInt(z);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerSleepUpdate(this);
    }

    public override int Size()
    {
        return 14;
    }
}
