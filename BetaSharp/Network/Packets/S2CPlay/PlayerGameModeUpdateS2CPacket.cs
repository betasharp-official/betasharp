using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerGameModeUpdateS2CPacket() : ExtendedProtocolPacket(PacketId.PlayerGameModeUpdateS2C)
{
    public string GameModeName { get; private set; } = "";

    public static PlayerGameModeUpdateS2CPacket Get(GameMode mode)
    {
        var p = Get<PlayerGameModeUpdateS2CPacket>(PacketId.PlayerGameModeUpdateS2C);
        p.GameModeName = mode.Name;
        return p;
    }

    public override void Read(NetworkStream stream) => GameModeName = stream.ReadString();
    public override void Write(NetworkStream stream) => stream.WriteString(GameModeName);
    public override void Apply(NetHandler handler) => handler.onPlayerGameModeUpdate(this);
    public override int Size() => 2 + GameModeName.Length;
}
