using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveFullPacket : PlayerMovePacket
{
    public PlayerMoveFullPacket() : base(PacketId.PlayerMoveFull)
    {
        changeLook = true;
        changePosition = true;
    }

    public static PlayerMoveFullPacket Get(double x, double y, double eyeHeight, double z, float yaw, float pitch, bool onGround)
    {
        var p = Get<PlayerMoveFullPacket>(PacketId.PlayerMoveFull);
        p.x = x;
        p.y = y;
        p.z = z;
        p.eyeHeight = eyeHeight;
        p.yaw = yaw;
        p.pitch = pitch;
        p.onGround = onGround;
        p.changeLook = true;
        p.changePosition = true;
        return p;
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
