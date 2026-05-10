namespace BetaSharp.Network.Packets.Play;

public class ScreenHandlerAcknowledgementPacket() : Packet(PacketId.ScreenHandlerAcknowledgement)
{
    public bool Accepted { get; private set; }
    public short ActionType { get; private set; }
    public int SyncId { get; private set; }

    public static ScreenHandlerAcknowledgementPacket Get(int syncId, short actionType, bool accepted)
    {
        ScreenHandlerAcknowledgementPacket p = Get<ScreenHandlerAcknowledgementPacket>(PacketId.ScreenHandlerAcknowledgement);
        p.SyncId = syncId;
        p.ActionType = actionType;
        p.Accepted = accepted;
        return p;
    }

    public override void Apply(NetHandler handler) => handler.onScreenHandlerAcknowledgement(this);

    public override void Read(Stream stream)
    {
        SyncId = (sbyte)stream.ReadByte();
        ActionType = stream.ReadShort();
        Accepted = (sbyte)stream.ReadByte() != 0;
    }

    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)SyncId);
        stream.WriteShort(ActionType);
        stream.WriteBoolean(Accepted);
    }

    public override int Size() => 4;
}
