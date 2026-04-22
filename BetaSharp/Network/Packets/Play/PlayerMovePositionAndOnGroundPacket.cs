using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMovePositionAndOnGroundPacket : PlayerMovePacket
{
    public PlayerMovePositionAndOnGroundPacket() : base(PacketId.PlayerMovePositionAndOnGround)
    {
        changePosition = true;
    }

    public static PlayerMovePositionAndOnGroundPacket Get(double x, double y, double eyeHeight, double z, bool onGround)
    {
        var p = Get<PlayerMovePositionAndOnGroundPacket>(PacketId.PlayerMovePositionAndOnGround);
        p.x = x;
        p.y = y;
        p.eyeHeight = eyeHeight;
        p.z = z;
        p.yaw = 0;
        p.pitch = 0;
        p.onGround = onGround;
        p.changePosition = true;
        p.changeLook = false;
        return p;
    }

    public override void Read(Stream stream)
    {
        x = stream.ReadDouble();
        y = stream.ReadDouble();
        eyeHeight = stream.ReadDouble();
        z = stream.ReadDouble();
        base.Read(stream);
    }

    public override void Write(Stream stream)
    {
        stream.WriteDouble(x);
        stream.WriteDouble(y);
        stream.WriteDouble(eyeHeight);
        stream.WriteDouble(z);
        base.Write(stream);
    }

    public override int Size()
    {
        return 33;
    }
}
