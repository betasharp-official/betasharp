using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class ScreenHandlerAcknowledgementPacket() : Packet(PacketId.ScreenHandlerAcknowledgement)
{
    public int syncId;
    public short actionType;
    public bool accepted;

    public ScreenHandlerAcknowledgementPacket(int syncId, short actionType, bool accepted) : this()
    {
        this.syncId = syncId;
        this.actionType = actionType;
        this.accepted = accepted;
    }

    public override void Apply(NetHandler handler)
    {
        handler.onScreenHandlerAcknowledgement(this);
    }

    public override void Read(NetworkStream stream)
    {
        syncId = (sbyte)stream.ReadByte();
        actionType = stream.ReadShort();
        accepted = (sbyte)stream.ReadByte() != 0;
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteByte((byte)syncId);
        stream.WriteShort((short)actionType);
        stream.WriteBoolean(accepted);
    }

    public override int Size()
    {
        return 4;
    }
}
