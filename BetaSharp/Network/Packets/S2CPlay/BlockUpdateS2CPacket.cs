using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Network.Packets.S2CPlay;

public class BlockUpdateS2CPacket() : Packet(PacketId.BlockUpdateS2C)
{
    public int BlockMetadata { get; private set; }
    public int BlockRawId { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }

    public static BlockUpdateS2CPacket Get(int x, int y, int z, IWorldContext world)
    {
        BlockUpdateS2CPacket p = Get<BlockUpdateS2CPacket>(PacketId.BlockUpdateS2C);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.BlockRawId = world.Reader.GetBlockId(x, y, z);
        p.BlockMetadata = world.Reader.GetBlockMeta(x, y, z);
        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Y = stream.ReadByte();
        Z = stream.ReadInt();
        BlockRawId = stream.ReadByte();
        BlockMetadata = stream.ReadByte();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(X);
        stream.WriteByte((byte)Y);
        stream.WriteInt(Z);
        stream.WriteByte((byte)BlockRawId);
        stream.WriteByte((byte)BlockMetadata);
    }

    public override void Apply(NetHandler handler) => handler.onBlockUpdate(this);

    public override int Size() => 11;
}
