using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerMovePacket(PacketId id = PacketId.PlayerMove) : Packet(id)
{
    public double x;
    public double y;
    public double z;
    public double eyeHeight;
    public float yaw;
    public float pitch;
    public bool onGround;
    public bool changePosition;
    public bool changeLook;

    public PlayerMovePacket(bool onGround) : this()
    {
        this.onGround = onGround;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerMove(this);
    }

    public override void Read(NetworkStream stream)
    {
        onGround = stream.ReadBoolean();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteBoolean(onGround);
    }

    public override int Size()
    {
        return 1;
    }
}
