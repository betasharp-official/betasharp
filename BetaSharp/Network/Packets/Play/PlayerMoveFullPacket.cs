namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveFullPacket : PlayerMovePacket
{
    public PlayerMoveFullPacket() : base(PacketId.PlayerMoveFull)
    {
        ChangeLook = true;
        ChangePosition = true;
    }

    public static PlayerMoveFullPacket Get(double x, double y, double eyeHeight, double z, float yaw, float pitch, bool onGround)
    {
        PlayerMoveFullPacket p = Get<PlayerMoveFullPacket>(PacketId.PlayerMoveFull);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.EyeHeight = eyeHeight;
        p.Yaw = yaw;
        p.Pitch = pitch;
        p.OnGround = onGround;
        p.ChangeLook = true;
        p.ChangePosition = true;
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadDouble();
        Y = stream.ReadDouble();
        EyeHeight = stream.ReadDouble();
        Z = stream.ReadDouble();
        Yaw = stream.ReadFloat();
        Pitch = stream.ReadFloat();
        base.Read(stream);
    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(X);
        stream.WriteDouble(Y);
        stream.WriteDouble(EyeHeight);
        stream.WriteDouble(Z);
        stream.WriteFloat(Yaw);
        stream.WriteFloat(Pitch);
        base.Write(stream);
    }

    public override int Size() => 41;
}
