using System.Net.Sockets;

namespace BetaSharp.Network.Packets.S2CPlay;

/// <summary>
/// Sent after all <see cref="RegistryDataS2CPacket"/>s during login. Signals that the
/// server has finished sending registry data and the client may validate and proceed.
/// </summary>
public class FinishConfigurationS2CPacket() : ExtendedProtocolPacket(PacketId.FinishConfigurationS2C)
{
    public override void Read(NetworkStream stream) { }
    public override void Write(NetworkStream stream) { }
    public override void Apply(NetHandler handler) => handler.onFinishConfiguration(this);
    public override int Size() => 0;
}
