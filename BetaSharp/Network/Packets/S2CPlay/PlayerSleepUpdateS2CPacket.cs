using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.S2CPlay;

public class PlayerSleepUpdateS2CPacket() : Packet(PacketId.PlayerSleepUpdateS2C)
{
    public int PlayerId { get; private set; }
    public int Status { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }

    public static PlayerSleepUpdateS2CPacket Get(Entity player, int status, int x, int y, int z)
    {
        PlayerSleepUpdateS2CPacket p = Get<PlayerSleepUpdateS2CPacket>(PacketId.PlayerSleepUpdateS2C);
        p.Status = status;
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.PlayerId = player.ID;
        return p;
    }

    public override void Read(Stream stream)
    {
        PlayerId = stream.ReadInt();
        Status = (sbyte)stream.ReadByte();
        X = stream.ReadInt();
        Y = (sbyte)stream.ReadByte();
        Z = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(PlayerId);
        stream.WriteByte((byte)Status);
        stream.WriteInt(X);
        stream.WriteByte((byte)Y);
        stream.WriteInt(Z);
    }

    public override void Apply(NetHandler handler) => handler.onPlayerSleepUpdate(this);

    public override int Size() => 14;
}
