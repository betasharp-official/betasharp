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

    public static PlayerMovePacket Get(bool onGround)
    {
        var p = Get<PlayerMovePacket>(PacketId.PlayerMove);
        p.x = 0;
        p.y = 0;
        p.z = 0;
        p.eyeHeight = 0;
        p.yaw = 0;
        p.pitch = 0;
        p.onGround = onGround;
        p.changePosition = false;
        p.changeLook = false;
        return p;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerMove(this);
    }

    public override void Read(Stream stream)
    {
        onGround = stream.ReadBoolean();
    }

    public override void Write(Stream stream)
    {
        stream.WriteBoolean(onGround);
    }

    public override int Size()
    {
        return 1;
    }
}
