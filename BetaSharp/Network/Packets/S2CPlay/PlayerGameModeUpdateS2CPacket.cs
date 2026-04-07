using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerGameModeUpdateS2CPacket() : ExtendedProtocolPacket(PacketId.PlayerGameModeUpdateS2C)
{
    public Namespace Namespace { get; private set; } = Namespace.BetaSharp;
    public string GameModeName { get; private set; } = "";

    public static PlayerGameModeUpdateS2CPacket Get(GameMode mode)
    {
        PlayerGameModeUpdateS2CPacket p = Get<PlayerGameModeUpdateS2CPacket>(PacketId.PlayerGameModeUpdateS2C);
        p.Namespace = mode.Namespace;
        p.GameModeName = mode.Name;
        return p;
    }

    public override void Read(Stream stream)
    {
        Namespace = stream.ReadNamespace();
        GameModeName = stream.ReadString();
    }

    public override void Write(Stream stream)
    {
        stream.WriteNamespace(Namespace);
        stream.WriteString(GameModeName);
    }

    public override void Apply(NetHandler handler) => handler.onPlayerGameModeUpdate(this);
    public override int Size() => 1 + GameModeName.Length + (Namespace.GetHashCode() == 0 ? 1 : Namespace.ToString().Length);
}
