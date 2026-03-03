using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMoveLookAndOnGroundPacket : PlayerMovePacket
{
    public PlayerMoveLookAndOnGroundPacket() : base(PacketId.PlayerMoveLookAndOnGround)
    {
        changeLook = true;
    }

    public PlayerMoveLookAndOnGroundPacket(float yaw, float pitch, bool onGround) : this()
    {
        base.yaw = yaw;
        base.pitch = pitch;
        base.onGround = onGround;
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
