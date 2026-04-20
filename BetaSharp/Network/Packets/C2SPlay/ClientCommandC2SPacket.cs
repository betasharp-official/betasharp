using System.Net.Sockets;
using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.C2SPlay;

public class ClientCommandC2SPacket() : Packet(PacketId.ClientCommandC2S)
{
    public int entityId;
    public int mode;

    public static ClientCommandC2SPacket Get(Entity ent, int mode)
    {
        var p = Get<ClientCommandC2SPacket>(PacketId.ClientCommandC2S);
        p.entityId = ent.ID;
        p.mode = mode;
        return p;
    }

    public override void Read(Stream stream)
    {
        entityId = stream.ReadInt();
        mode = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
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
