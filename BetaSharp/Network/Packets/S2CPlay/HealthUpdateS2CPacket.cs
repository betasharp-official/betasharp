using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class HealthUpdateS2CPacket() : Packet(PacketId.HealthUpdateS2C)
{
    public int healthMP;

    public static HealthUpdateS2CPacket Get(int health)
    {
        var p = Get<HealthUpdateS2CPacket>(PacketId.HealthUpdateS2C);
        p.healthMP = health;
        return p;
    }

    public override void Read(Stream stream)
    {
        healthMP = stream.ReadShort();
    }

    public override void Write(Stream stream)
    {
        stream.WriteShort((short)healthMP);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onHealthUpdate(this);
    }

    public override int Size()
    {
        return 2;
    }
}
