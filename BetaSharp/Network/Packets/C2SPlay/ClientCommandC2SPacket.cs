using BetaSharp.Entities;

namespace BetaSharp.Network.Packets.C2SPlay;

public class ClientCommandC2SPacket() : Packet(PacketId.ClientCommandC2S), IPacketEntity
{
    public int Mode { get; private set; }
    public int EntityId { get; private set; }

    public static ClientCommandC2SPacket Get(Entity ent, int mode)
    {
        ClientCommandC2SPacket p = Get<ClientCommandC2SPacket>(PacketId.ClientCommandC2S);
        p.EntityId = ent.ID;
        p.Mode = mode;
        return p;
    }

    public override void Read(Stream stream)
    {
        EntityId = stream.ReadInt();
        Mode = (sbyte)stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EntityId);
        stream.WriteByte((byte)Mode);
    }

    public override void Apply(NetHandler handler) => handler.handleClientCommand(this);

    public override int Size() => 5;
}
