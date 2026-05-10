namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerActionC2SPacket() : Packet(PacketId.PlayerActionC2S)
{
    public int Action { get; private set; }
    public int Direction { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }

    public static PlayerActionC2SPacket Get(int action, int x, int y, int z, int direction)
    {
        PlayerActionC2SPacket p = Get<PlayerActionC2SPacket>(PacketId.PlayerActionC2S);
        p.Action = action;
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.Direction = direction;
        return p;
    }

    public override void Read(Stream stream)
    {
        Action = stream.ReadByte();
        X = stream.ReadInt();
        Y = stream.ReadByte();
        Z = stream.ReadInt();
        Direction = stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)Action);
        stream.WriteInt(X);
        stream.WriteByte((byte)Y);
        stream.WriteInt(Z);
        stream.WriteByte((byte)Direction);
    }

    public override void Apply(NetHandler handler) => handler.handlePlayerAction(this);

    public override int Size() => 11;
}
