using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMovePositionAndOnGroundPacket : PlayerMovePacket
{
    public PlayerMovePositionAndOnGroundPacket() : base(PacketId.PlayerMovePositionAndOnGround)
    {
        changePosition = true;
    }

    public PlayerMovePositionAndOnGroundPacket(double x, double y, double eyeHeight, double z, bool onGround) : this()
    {
        base.x = x;
        base.y = y;
        base.eyeHeight = eyeHeight;
        base.z = z;
        base.onGround = onGround;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadDouble();
        y = stream.ReadDouble();
        eyeHeight = stream.ReadDouble();
        z = stream.ReadDouble();
        base.Read(stream);
    }

    public override void Write(NetworkStream stream)
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
