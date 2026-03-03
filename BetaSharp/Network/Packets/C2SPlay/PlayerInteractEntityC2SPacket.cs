using System.Net.Sockets;

namespace BetaSharp.Network.Packets.C2SPlay;

public class PlayerInteractEntityC2SPacket() : Packet(PacketId.PlayerInteractEntityC2S)
{
    public int playerId;
    public int entityId;
    public int isLeftClick;

    public PlayerInteractEntityC2SPacket(int playerId, int entityId, int isLeftClick) : this()
    {
        this.playerId = playerId;
        this.entityId = entityId;
        this.isLeftClick = isLeftClick;
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
