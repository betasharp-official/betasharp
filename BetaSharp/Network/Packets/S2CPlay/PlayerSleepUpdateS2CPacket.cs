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

    public PlayerSleepUpdateS2CPacket(Entity player, int status, int x, int y, int z) : this()
    {
        this.status = status;
        this.x = x;
        this.y = y;
        this.z = z;
        this.id = player.id;
    }

    public override void Read(NetworkStream stream)
    {
        id = stream.ReadInt();
        status = (sbyte)stream.ReadByte();
        x = stream.ReadInt();
        y = (sbyte)stream.ReadByte();
        z = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
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
