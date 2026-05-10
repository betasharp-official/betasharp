namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveLookAndOnGroundPacket : PlayerMovePacket
{
    public PlayerMoveLookAndOnGroundPacket() : base(PacketId.PlayerMoveLookAndOnGround) => ChangeLook = true;

    public static PlayerMoveLookAndOnGroundPacket Get(float yaw, float pitch, bool onGround)
    {
        PlayerMoveLookAndOnGroundPacket p = Get<PlayerMoveLookAndOnGroundPacket>(PacketId.PlayerMoveLookAndOnGround);
        p.X = 0;
        p.Y = 0;
        p.Z = 0;
        p.EyeHeight = 0;
        p.Yaw = yaw;
        p.Pitch = pitch;
        p.OnGround = onGround;
        p.ChangePosition = false;
        p.ChangeLook = true;
        return p;
    }

    public override void Read(Stream stream)
    {
        Yaw = stream.ReadFloat();
        Pitch = stream.ReadFloat();
        base.Read(stream);
    }

    public override void Write(Stream stream)
    {
        stream.WriteFloat(Yaw);
        stream.WriteFloat(Pitch);
        base.Write(stream);
    }

    public override int Size() => 9;
}
