using System.Net.Sockets;

namespace BetaSharp.Network.Packets;

public class LoginHelloPacket() : Packet(PacketId.LoginHello)
{
    public const long BETASHARP_CLIENT_SIGNATURE = 0x627368617270; // "bsharp" in hex. Used to identify BetaSharp clients for future protocol extensions without breaking vanilla compatibility.

    public int protocolVersion;
    public string username;
    public long worldSeed;
    public sbyte dimensionId;

    public LoginHelloPacket(string username, int protocolVersion, long worldSeed, sbyte dimensionId) : this()
    {
        this.username = username;
        this.protocolVersion = protocolVersion;
        this.worldSeed = worldSeed;
        this.dimensionId = dimensionId;
    }

    public LoginHelloPacket(string username, int protocolVersion) : this()
    {
        this.username = username;
        this.protocolVersion = protocolVersion;
    }

    public override void Read(NetworkStream stream)
    {
        protocolVersion = stream.ReadInt();
        username = stream.ReadLongString(16);
        worldSeed = stream.ReadLong();
        dimensionId = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(protocolVersion);
        stream.WriteLongString(username);
        stream.WriteLong(worldSeed);
        stream.WriteByte((byte)dimensionId);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onHello(this);
    }

    public override int Size()
    {
        return 4 + username.Length + 4 + 5;
    }
}
