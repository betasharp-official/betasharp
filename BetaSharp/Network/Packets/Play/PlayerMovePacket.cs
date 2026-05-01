namespace BetaSharp.Network.Packets.Play;

public class PlayerMovePacket(PacketId id = PacketId.PlayerMove) : Packet(id)
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double EyeHeight { get; set; }
    public float Yaw { get; protected set; }
    public float Pitch { get; protected set; }
    public bool OnGround { get; protected set; }
    public bool ChangePosition { get; set; }
    public bool ChangeLook { get; protected set; }

    public static PlayerMovePacket Get(bool onGround)
    {
        PlayerMovePacket p = Get<PlayerMovePacket>(PacketId.PlayerMove);
        p.X = 0;
        p.Y = 0;
        p.Z = 0;
        p.EyeHeight = 0;
        p.Yaw = 0;
        p.Pitch = 0;
        p.OnGround = onGround;
        p.ChangePosition = false;
        p.ChangeLook = false;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onPlayerMove(this);

    public override void Read(Stream stream) => OnGround = stream.ReadBoolean();

    public override void Write(Stream stream) => stream.WriteBoolean(OnGround);

    public override int Size() => 1;
}
