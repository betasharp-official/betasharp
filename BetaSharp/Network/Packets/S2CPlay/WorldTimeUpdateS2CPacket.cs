namespace BetaSharp.Network.Packets.S2CPlay;

public class WorldTimeUpdateS2CPacket() : Packet(PacketId.WorldTimeUpdateS2C)
{
    public long Time { get; private set; }

    public static WorldTimeUpdateS2CPacket Get(long time)
    {
        WorldTimeUpdateS2CPacket p = Get<WorldTimeUpdateS2CPacket>(PacketId.WorldTimeUpdateS2C);
        p.Time = time;
        return p;
    }

    public override void Read(Stream stream) => Time = stream.ReadLong();

    public override void Write(Stream stream) => stream.WriteLong(Time);

    public override void Apply(NetHandler handler) => handler.onWorldTimeUpdate(this);

    public override int Size() => 8;
}
