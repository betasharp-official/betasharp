namespace BetaSharp.Network.Packets.Play;

public interface IPlayerMoveLook : IPacketPlayerMove
{
    float Yaw { get; }
    float Pitch { get; }
}

public class PlayerMoveLookAndOnGroundPacket() : PacketPlayerMoveAbstract(PacketId.PlayerMoveLookAndOnGround), IPlayerMoveLook
{
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }

    public static PlayerMoveLookAndOnGroundPacket Get(float yaw, float pitch, bool onGround)
    {
        PlayerMoveLookAndOnGroundPacket p = Get<PlayerMoveLookAndOnGroundPacket>(PacketId.PlayerMoveLookAndOnGround);
        p.Yaw = yaw;
        p.Pitch = pitch;
        p.OnGround = onGround;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onPlayerMove(this);

    public override void Read(Stream stream)
    {
        Yaw = stream.ReadFloat();
        Pitch = stream.ReadFloat();
        OnGround = stream.ReadBoolean();
    }

    public override void Write(Stream stream)
    {
        stream.WriteFloat(Yaw);
        stream.WriteFloat(Pitch);
        stream.WriteBoolean(OnGround);
    }

    public override int Size() => 9;
}
