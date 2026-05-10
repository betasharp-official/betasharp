namespace BetaSharp.Network.Packets.Play;

public class PlayerMovePositionAndOnGroundPacket : PlayerMovePacket
{
    public PlayerMovePositionAndOnGroundPacket() : base(PacketId.PlayerMovePositionAndOnGround) => ChangePosition = true;

    public static PlayerMovePositionAndOnGroundPacket Get(double x, double y, double eyeHeight, double z, bool onGround)
    {
        PlayerMovePositionAndOnGroundPacket p = Get<PlayerMovePositionAndOnGroundPacket>(PacketId.PlayerMovePositionAndOnGround);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.EyeHeight = eyeHeight;
        p.Yaw = 0;
        p.Pitch = 0;
        p.OnGround = onGround;
        p.ChangePosition = true;
        p.ChangeLook = false;
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadDouble();
        Y = stream.ReadDouble();
        EyeHeight = stream.ReadDouble();
        Z = stream.ReadDouble();
        base.Read(stream);
    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(X);
        stream.WriteDouble(Y);
        stream.WriteDouble(EyeHeight);
        stream.WriteDouble(Z);
        base.Write(stream);
    }

    public override int Size() => 33;
}
