namespace BetaSharp.Network.Packets.S2CPlay;

public class WorldEventS2CPacket() : Packet(PacketId.WorldEventS2C)
{
    public int Data { get; private set; }
    public int EventId { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }

    public static WorldEventS2CPacket Get(int eventId, int x, int y, int z, int data)
    {
        WorldEventS2CPacket p = Get<WorldEventS2CPacket>(PacketId.WorldEventS2C);
        p.EventId = eventId;
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.Data = data;
        return p;
    }

    public override void Read(Stream stream)
    {
        EventId = stream.ReadInt();
        X = stream.ReadInt();
        Y = (sbyte)stream.ReadByte();
        Z = stream.ReadInt();
        Data = stream.ReadInt();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(EventId);
        stream.WriteInt(X);
        stream.WriteByte((byte)Y);
        stream.WriteInt(Z);
        stream.WriteInt(Data);
    }

    public override void Apply(NetHandler handler) => handler.onWorldEvent(this);

    public override int Size() => 20;
}
