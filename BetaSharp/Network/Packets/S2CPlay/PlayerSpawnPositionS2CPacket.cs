namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerSpawnPositionS2CPacket() : Packet(PacketId.PlayerSpawnPositionS2C)
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }

    public static PlayerSpawnPositionS2CPacket Get(int x, int y, int z)
    {
        PlayerSpawnPositionS2CPacket p = Get<PlayerSpawnPositionS2CPacket>(PacketId.PlayerSpawnPositionS2C);
        p.X = x;
        p.Y = y;
        p.Z = z;
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Y = stream.ReadInt();
        Z = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(X);
        stream.WriteInt(Y);
        stream.WriteInt(Z);
    }

    public override void Apply(NetHandler handler) => handler.onPlayerSpawnPosition(this);

    public override int Size() => 12;
}
