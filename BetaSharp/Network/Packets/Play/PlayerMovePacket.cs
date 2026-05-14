namespace BetaSharp.Network.Packets.Play;

public class PlayerMovePacket() : PacketPlayerMoveAbstract(PacketId.PlayerMove)
{
    public static PlayerMovePacket Get(bool onGround)
    {
        PlayerMovePacket p = Get<PlayerMovePacket>(PacketId.PlayerMove);
        p.OnGround = onGround;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onPlayerMove(this);

    public override void Read(Stream stream) => OnGround = stream.ReadBoolean();

    public override void Write(Stream stream) => stream.WriteBoolean(OnGround);

    public override int Size() => 1;
}
