using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class IncreaseStatS2CPacket() : Packet(PacketId.IncreaseStatS2C)
{
    public int statId;
    public int amount;

    public static IncreaseStatS2CPacket Get(int statId, int amount)
    {
        var p = Get<IncreaseStatS2CPacket>(PacketId.IncreaseStatS2C);
        p.statId = statId;
        p.amount = amount;
        return p;
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
