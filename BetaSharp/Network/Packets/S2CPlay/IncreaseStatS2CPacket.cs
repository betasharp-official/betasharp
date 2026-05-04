namespace BetaSharp.Network.Packets.S2CPlay;

public class IncreaseStatS2CPacket() : Packet(PacketId.IncreaseStatS2C)
{
    public int Amount { get; private set; }
    public int StatId { get; private set; }

    public static IncreaseStatS2CPacket Get(int statId, int amount)
    {
        IncreaseStatS2CPacket p = Get<IncreaseStatS2CPacket>(PacketId.IncreaseStatS2C);
        p.StatId = statId;
        p.Amount = amount;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onIncreaseStat(this);

    public override void Read(Stream stream)
    {
        StatId = stream.ReadInt();
        Amount = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(StatId);
        stream.WriteByte((byte)Amount);
    }

    public override int Size() => 6;
}
