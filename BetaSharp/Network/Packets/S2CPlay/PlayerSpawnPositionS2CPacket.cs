using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerSpawnPositionS2CPacket() : Packet(PacketId.PlayerSpawnPositionS2C)
{
    public int x;
    public int y;
    public int z;

    public static PlayerSpawnPositionS2CPacket Get(int x, int y, int z)
    {
        var p = Get<PlayerSpawnPositionS2CPacket>(PacketId.PlayerSpawnPositionS2C);
        p.x = x;
        p.y = y;
        p.z = z;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadInt();
        y = stream.ReadInt();
        z = stream.ReadInt();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(x);
        stream.WriteInt(y);
        stream.WriteInt(z);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerSpawnPosition(this);
    }

    public override int Size()
    {
        return 12;
    }
}
