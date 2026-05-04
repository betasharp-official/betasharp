namespace BetaSharp.Network.Packets.S2CPlay;

public class GameStateChangeS2CPacket() : Packet(PacketId.GameStateChangeS2C)
{
    public static readonly string?[] Reasons = ["tile.bed.notValid", null, null];
    public int Reason { get; private set; }

    public static GameStateChangeS2CPacket Get(int reason)
    {
        GameStateChangeS2CPacket p = Get<GameStateChangeS2CPacket>(PacketId.GameStateChangeS2C);
        p.Reason = reason;
        return p;
    }

    public override void Read(Stream stream) => Reason = (sbyte)stream.ReadByte();

    public override void Write(Stream stream) => stream.WriteByte((byte)Reason);

    public override void Apply(NetHandler handler) => handler.onGameStateChange(this);

    public override int Size() => 1;
}
