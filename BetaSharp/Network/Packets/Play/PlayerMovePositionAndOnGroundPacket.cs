namespace BetaSharp.Network.Packets.Play;

public interface IPlayerMovePos : IPacketPlayerMove
{
    double X { get; set; }
    double Y { get; set; }
    double Z { get; set; }
    double EyeHeight { get; set;}
}

public class PlayerMovePositionAndOnGroundPacket() : PacketPlayerMoveAbstract(PacketId.PlayerMovePositionAndOnGround), IPlayerMovePos
{

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double EyeHeight { get; set; }

    public static PlayerMovePositionAndOnGroundPacket Get(double x, double y, double eyeHeight, double z, bool onGround)
    {
        PlayerMovePositionAndOnGroundPacket p = Get<PlayerMovePositionAndOnGroundPacket>(PacketId.PlayerMovePositionAndOnGround);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.EyeHeight = eyeHeight;
        p.OnGround = onGround;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onPlayerMove(this);

    public override void Read(Stream stream)
    {
        X = stream.ReadDouble();
        Y = stream.ReadDouble();
        EyeHeight = stream.ReadDouble();
        Z = stream.ReadDouble();
        OnGround = stream.ReadBoolean();
    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(X);
        stream.WriteDouble(Y);
        stream.WriteDouble(EyeHeight);
        stream.WriteDouble(Z);
        stream.WriteBoolean(OnGround);
    }

    public override int Size() => 33;
}
