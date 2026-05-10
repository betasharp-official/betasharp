namespace BetaSharp.Network.Packets.S2CPlay;

public class HealthUpdateS2CPacket() : Packet(PacketId.HealthUpdateS2C)
{
    public int HealthMp { get; private set; }

    public static HealthUpdateS2CPacket Get(int health)
    {
        HealthUpdateS2CPacket p = Get<HealthUpdateS2CPacket>(PacketId.HealthUpdateS2C);
        p.HealthMp = health;
        return p;
    }

    public override void Read(Stream stream) => HealthMp = stream.ReadShort();

    public override void Write(Stream stream) => stream.WriteShort((short)HealthMp);

    public override void Apply(NetHandler handler) => handler.onHealthUpdate(this);

    public override int Size() => 2;
}
