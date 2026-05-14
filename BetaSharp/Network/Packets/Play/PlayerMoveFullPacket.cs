namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveFullPacket() : PacketPlayerMoveAbstract(PacketId.PlayerMoveFull), IPlayerMoveLook, IPlayerMovePos
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double EyeHeight { get; set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }

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
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onPlayerMove(this);

    public override void Read(Stream stream)
    {
        X = stream.ReadDouble();
        Y = stream.ReadDouble();
        EyeHeight = stream.ReadDouble();
        Z = stream.ReadDouble();
        Yaw = stream.ReadFloat();
        Pitch = stream.ReadFloat();
        OnGround = stream.ReadBoolean();
    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(X);
        stream.WriteDouble(Y);
        stream.WriteDouble(EyeHeight);
        stream.WriteDouble(Z);
        stream.WriteFloat(Yaw);
        stream.WriteFloat(Pitch);
        stream.WriteBoolean(OnGround);
    }

    public override int Size() => 41;
}
