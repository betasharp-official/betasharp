using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class IncreaseStatS2CPacket() : Packet(PacketId.IncreaseStatS2C)
{
    public int statId;
    public int amount;

    public IncreaseStatS2CPacket(int statId, int amount) : this()
    {
        this.statId = statId;
        this.amount = amount;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onIncreaseStat(this);
    }

    public override void Read(NetworkStream stream)
    {
        statId = stream.ReadInt();
        amount = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(statId);
        stream.WriteByte((byte)amount);
    }

    public override int Size()
    {
        return 6;
    }
}
