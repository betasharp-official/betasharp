using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.C2SPlay;

public class ClientCommandC2SPacket() : Packet(PacketId.ClientCommandC2S)
{
    public int entityId;
    public int mode;

    public ClientCommandC2SPacket(Entity ent, int mode) : this()
    {
        entityId = ent.id;
        this.mode = mode;
    }

    public override void Read(NetworkStream stream)
    {
        entityId = stream.ReadInt();
        mode = (sbyte)stream.ReadByte();
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(entityId);
        stream.WriteByte((byte)mode);
    }

    public override void Apply(NetHandler handler)
    {
        handler.handleClientCommand(this);
    }

    public override int Size()
    {
        return 5;
    }
}
