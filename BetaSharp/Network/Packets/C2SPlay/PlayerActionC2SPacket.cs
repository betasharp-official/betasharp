using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerActionC2SPacket() : Packet(PacketId.PlayerActionC2S)
{
    public int x;
    public int y;
    public int z;
    public int direction;
    public int action;

    public PlayerActionC2SPacket(int action, int x, int y, int z, int direction) : this()
    {
        this.action = action;
        this.x = x;
        this.y = y;
        this.z = z;
        this.direction = direction;
    }

    public override void Read(NetworkStream stream)
    {
        action = stream.ReadByte();
        x = stream.ReadInt();
        y = stream.ReadByte();
        z = stream.ReadInt();
        direction = stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)action);
        stream.WriteInt(x);
        stream.WriteByte((byte)y);
        stream.WriteInt(z);
        stream.WriteByte((byte)direction);
    }

    public override void Apply(NetHandler handler)
    {
        handler.handlePlayerAction(this);
    }

    public override int Size()
    {
        return 11;
    }
}
