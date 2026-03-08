using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class PlayerRespawnPacket() : Packet(PacketId.PlayerRespawn)
{
    public sbyte dimensionId;

    public static PlayerRespawnPacket Get(sbyte dimensionId)
    {
        var p = Get<PlayerRespawnPacket>(PacketId.PlayerRespawn);
        p.dimensionId = dimensionId;
        return p;
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
