using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInputC2SPacket() : Packet(PacketId.PlayerInputC2S)
{
    private float sideways;
    private float forward;
    private bool jumping;
    private bool sneaking;
    private float pitch;
    private float yaw;

    public override void Read(NetworkStream stream)
    {
        sideways = stream.ReadFloat();
        forward = stream.ReadFloat();
        pitch = stream.ReadFloat();
        yaw = stream.ReadFloat();
        jumping = stream.ReadBoolean();
        sneaking = stream.ReadBoolean();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteFloat(sideways);
        stream.WriteFloat(forward);
        stream.WriteFloat(pitch);
        stream.WriteFloat(yaw);
        stream.WriteBoolean(jumping);
        stream.WriteBoolean(sneaking);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerInput(this);
    }

    public override int Size()
    {
        return 18;
    }

    public float getSideways()
    {
        return sideways;
    }

    public float getPitch()
    {
        return pitch;
    }

    public float getForward()
    {
        return forward;
    }

    public float getYaw()
    {
        return yaw;
    }

    public bool isJumping()
    {
        return jumping;
    }

    public bool isSneaking()
    {
        return sneaking;
    }
}
