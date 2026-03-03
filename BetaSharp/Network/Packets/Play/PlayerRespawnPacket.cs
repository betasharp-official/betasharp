using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerRespawnPacket() : Packet(PacketId.PlayerRespawn)
{
    public sbyte dimensionId;

    public PlayerRespawnPacket(sbyte dimensionId) : this()
    {
        this.dimensionId = dimensionId;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onPlayerRespawn(this);
    }

    public override void Read(NetworkStream stream)
    {
        dimensionId = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)dimensionId);
    }

    public override int Size()
    {
        return 1;
    }
}
