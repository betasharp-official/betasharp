using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class HealthUpdateS2CPacket() : Packet(PacketId.HealthUpdateS2C)
{
    public int healthMP;

    public HealthUpdateS2CPacket(int health) : this()
    {
        healthMP = health;
    }

    public override void Read(NetworkStream stream)
    {
        healthMP = stream.ReadShort();
    }

    public override void Write(NetworkStream stream)
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
