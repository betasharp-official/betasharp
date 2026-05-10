using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkDeltaUpdateS2CPacket() : Packet(PacketId.ChunkDeltaUpdateS2C)
{
    public int Count { get; private set; }
    public byte[] BlockMetadata { get; private set; } = [];
    public byte[] BlockRawIds { get; private set; } = [];
    public short[] Positions { get; private set; } = [];
    public int X { get; private set; }
    public int Z { get; private set; }

    public static ChunkDeltaUpdateS2CPacket Get(int x, int z, short[] positions, int size, IWorldContext world)
    {
        ChunkDeltaUpdateS2CPacket p = Get<ChunkDeltaUpdateS2CPacket>(PacketId.ChunkDeltaUpdateS2C);
        p.X = x;
        p.Z = z;
        p.Count = size;
        p.Positions = new short[size];
        p.BlockRawIds = new byte[size];
        p.BlockMetadata = new byte[size];
        Chunk chunk = world.ChunkHost.GetChunk(x, z);

        for (int i = 0; i < size; i++)
        {
            int blockX = (positions[i] >> 12) & 15;
            int blockZ = (positions[i] >> 8) & 15;
            int blockY = positions[i] & 255;
            p.Positions[i] = positions[i];
            p.BlockRawIds[i] = (byte)chunk.GetBlockId(blockX, blockY, blockZ);
            p.BlockMetadata[i] = (byte)chunk.GetBlockMeta(blockX, blockY, blockZ);
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Z = stream.ReadInt();
        Count = stream.ReadShort() & '\uffff';
        Positions = new short[Count];

        BlockRawIds = new byte[Count];
        BlockMetadata = new byte[Count];

        for (int i = 0; i < Count; ++i)
        {
            Positions[i] = stream.ReadShort();
        }

        stream.ReadExactly(BlockRawIds);
        stream.ReadExactly(BlockMetadata);
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(X);
        stream.WriteInt(Z);
        stream.WriteShort((short)Count);

        for (int i = 0; i < Count; ++i)
        {
            stream.WriteShort(Positions[i]);
        }

        stream.Write(BlockRawIds);
        stream.Write(BlockMetadata);
    }

    public override void Apply(NetHandler handler) => handler.onChunkDeltaUpdate(this);

    public override int Size() => 10 + Count * 4;
}
