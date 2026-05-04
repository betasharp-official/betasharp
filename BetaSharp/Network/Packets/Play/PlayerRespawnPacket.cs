namespace BetaSharp.Network.Packets.Play;

public class PlayerRespawnPacket() : Packet(PacketId.PlayerRespawn)
{
    public sbyte DimensionId { get; private set; }

    public static PlayerRespawnPacket Get(sbyte dimensionId)
    {
        PlayerRespawnPacket p = Get<PlayerRespawnPacket>(PacketId.PlayerRespawn);
        p.DimensionId = dimensionId;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onPlayerRespawn(this);

    public override void Read(Stream stream) => DimensionId = (sbyte)stream.ReadByte();

    public override void Write(Stream stream) => stream.WriteByte((byte)DimensionId);

    public override int Size() => 1;
}
