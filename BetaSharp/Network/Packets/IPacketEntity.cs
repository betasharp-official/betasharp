namespace BetaSharp.Network.Packets;

public interface IPacketEntity
{
    protected const int PacketBaseEntitySize = 4;
    int EntityId { get; }
}
