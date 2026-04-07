using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerActionC2SPacket() : Packet(PacketId.PlayerActionC2S)
{
    public int x;
    public int y;
    public int z;
    public int direction;
    public int action;

    public static PlayerActionC2SPacket Get(int action, int x, int y, int z, int direction)
    {
        var p = Get<PlayerActionC2SPacket>(PacketId.PlayerActionC2S);
        p.action = action;
        p.x = x;
        p.y = y;
        p.z = z;
        p.direction = direction;
        return p;
    }

    public override void Read(Stream stream)
    {
        action = stream.ReadByte();
        x = stream.ReadInt();
        y = stream.ReadByte();
        z = stream.ReadInt();
        direction = stream.ReadByte();
    }

    public override void Write(Stream stream)
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
