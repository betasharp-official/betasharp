using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInteractEntityC2SPacket() : Packet(PacketId.PlayerInteractEntityC2S)
{
    public int playerId;
    public int entityId;
    public int isLeftClick;

    public static PlayerInteractEntityC2SPacket Get(int playerId, int entityId, int isLeftClick)
    {
        var p = Get<PlayerInteractEntityC2SPacket>(PacketId.PlayerInteractEntityC2S);
        p.playerId = playerId;
        p.entityId = entityId;
        p.isLeftClick = isLeftClick;
        return p;
    }

    public override void Read(NetworkStream stream)
    {
        playerId = stream.ReadInt();
        entityId = stream.ReadInt();
        isLeftClick = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(playerId);
        stream.WriteInt(entityId);
        stream.WriteByte((byte)isLeftClick);
    }

    public override void Apply(NetHandler handler)
    {
        handler.handleInteractEntity(this);
    }

    public override int Size()
    {
        return 9;
    }
}
