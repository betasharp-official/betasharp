namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkStatusUpdateS2CPacket() : Packet(PacketId.ChunkStatusUpdateS2C)
{
    public bool Load { get; private set; }
    public int X { get; private set; }
    public int Z { get; private set; }

    public static ChunkStatusUpdateS2CPacket Get(int x, int z, bool load)
    {
        ChunkStatusUpdateS2CPacket p = Get<ChunkStatusUpdateS2CPacket>(PacketId.ChunkStatusUpdateS2C);
        p.X = x;
        p.Z = z;
        p.Load = load;
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Z = stream.ReadInt();
        Load = stream.ReadByte() != 0;
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(X);
        stream.WriteInt(Z);
        stream.WriteBoolean(Load);
    }

    public override void Apply(NetHandler handler) => handler.onChunkStatusUpdate(this);

    public override int Size() => 9;
}
