using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveFullPacket : PlayerMovePacket
{
    public PlayerMoveFullPacket() : base(PacketId.PlayerMoveFull)
    {
        changeLook = true;
        changePosition = true;
    }

    public PlayerMoveFullPacket(double x, double y, double eyeHeight, double z, float yaw, float pitch, bool onGround) : this()
    {
        base.x = x;
        base.y = y;
        base.z = z;
        base.eyeHeight = eyeHeight;
        base.yaw = yaw;
        base.pitch = pitch;
        base.onGround = onGround;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadDouble();
        y = stream.ReadDouble();
        eyeHeight = stream.ReadDouble();
        z = stream.ReadDouble();
        yaw = stream.ReadFloat();
        pitch = stream.ReadFloat();
        base.Read(stream);
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteDouble(x);
        stream.WriteDouble(y);
        stream.WriteDouble(eyeHeight);
        stream.WriteDouble(z);
        stream.WriteFloat(yaw);
        stream.WriteFloat(pitch);
        base.Write(stream);
    }

    public override int Size()
    {
        return 41;
    }
}
