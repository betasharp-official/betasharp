using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveLookAndOnGroundPacket : PlayerMovePacket
{
    public PlayerMoveLookAndOnGroundPacket() : base(PacketId.PlayerMoveLookAndOnGround)
    {
        changeLook = true;
    }

    public static PlayerMoveLookAndOnGroundPacket Get(float yaw, float pitch, bool onGround)
    {
        var p = Get<PlayerMoveLookAndOnGroundPacket>(PacketId.PlayerMoveLookAndOnGround);
        p.x = 0;
        p.y = 0;
        p.z = 0;
        p.eyeHeight = 0;
        p.yaw = yaw;
        p.pitch = pitch;
        p.onGround = onGround;
        p.changePosition = false;
        p.changeLook = true;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        yaw = stream.ReadFloat();
        pitch = stream.ReadFloat();
        base.Read(stream);
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteFloat(yaw);
        stream.WriteFloat(pitch);
        base.Write(stream);
    }

    public override int Size()
    {
        return 9;
    }
}
