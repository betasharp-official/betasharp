using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class GameStateChangeS2CPacket() : Packet(PacketId.GameStateChangeS2C)
{
    public static readonly string[] REASONS = ["tile.bed.notValid", null, null];
    public int reason;

    public GameStateChangeS2CPacket(int reason) : this()
    {
        this.reason = reason;
    }

    public override void Read(NetworkStream stream)
    {
        reason = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
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
