using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class WorldTimeUpdateS2CPacket() : Packet(PacketId.WorldTimeUpdateS2C)
{
    public long time;

    public static WorldTimeUpdateS2CPacket Get(long time)
    {
        var p = Get<WorldTimeUpdateS2CPacket>(PacketId.WorldTimeUpdateS2C);
        p.time = time;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        time = stream.ReadLong();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteLong(time);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onWorldTimeUpdate(this);
    }

    public override int Size()
    {
        return 8;
    }
}
