namespace BetaSharp.Network.Packets.S2CPlay;

/// <summary>
/// Sent after all <see cref="RegistryDataS2CPacket"/>s during login. Signals that the
/// server has finished sending registry data and the client may validate and proceed.
/// </summary>
public class FinishConfigurationS2CPacket() : ExtendedProtocolPacket(PacketId.FinishConfigurationS2C)
{
    public static FinishConfigurationS2CPacket Get() => Get<FinishConfigurationS2CPacket>(PacketId.FinishConfigurationS2C);
    public override void Read(Stream stream) { }
    public override void Write(Stream stream) { }
    public override void Apply(NetHandler handler) => handler.onFinishConfiguration(this);
    public override int Size() => 0;
}
