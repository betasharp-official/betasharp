namespace BetaSharp.Network.Packets;

public interface IPacketPlayerMove
{
    public bool OnGround { get; }
}

public abstract class PacketPlayerMoveAbstract : Packet, IPacketPlayerMove
{
    public bool OnGround { get; protected set; }
    protected PacketPlayerMoveAbstract(byte id) : base(id) { }
    protected PacketPlayerMoveAbstract(PacketId id) : base(id) { }
}
