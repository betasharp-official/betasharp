namespace BetaSharp.Network.Packets;

public class LoginHelloPacket() : Packet(PacketId.LoginHello)
{
    public const long BETASHARP_CLIENT_SIGNATURE = 0x627368617270; // "bsharp" in hex. Used to identify BetaSharp clients for future protocol extensions without breaking vanilla compatibility.

    public int ProtocolVersion { get; private set; }
    public string Username { get; set; } = "";
    public long WorldSeed { get; private set; }
    public sbyte DimensionId { get; private set; }

    public static LoginHelloPacket Get(string username, int protocolVersion, long worldSeed, sbyte dimensionId)
    {
        LoginHelloPacket p = Get<LoginHelloPacket>(PacketId.LoginHello);
        p.Username = username;
        p.ProtocolVersion = protocolVersion;
        p.WorldSeed = worldSeed;
        p.DimensionId = dimensionId;
        return p;
    }

    public override void Read(Stream stream)
    {
        ProtocolVersion = stream.ReadInt();
        Username = stream.ReadLongString(16);
        WorldSeed = stream.ReadLong();
        DimensionId = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(ProtocolVersion);
        stream.WriteLongString(Username);
        stream.WriteLong(WorldSeed);
        stream.WriteByte((byte)DimensionId);
    }

    public override void Apply(NetHandler handler) => handler.onHello(this);

    public override int Size() => 4 + Username.Length + 4 + 5;
}
