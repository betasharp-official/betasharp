using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class GameStateChangeS2CPacket() : Packet(PacketId.GameStateChangeS2C)
{
    public static readonly string[] REASONS = ["tile.bed.notValid", null, null];
    public int reason;

    public static GameStateChangeS2CPacket Get(int reason)
    {
        var p = Get<GameStateChangeS2CPacket>(PacketId.GameStateChangeS2C);
        p.reason = reason;
        return p;
    }

    public override void Read(Stream stream)
    {
        reason = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)reason);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onGameStateChange(this);
    }

    public override int Size()
    {
        return 1;
    }
}
