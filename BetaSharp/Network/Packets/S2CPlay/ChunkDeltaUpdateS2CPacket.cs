using System.Net.Sockets;
using BetaSharp.Worlds.Chunks;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkDeltaUpdateS2CPacket() : Packet(PacketId.ChunkDeltaUpdateS2C)
{
    public int x;
    public int z;
    public short[] positions;
    public byte[] blockRawIds;
    public byte[] blockMetadata;
    public int _size;

    public static ChunkDeltaUpdateS2CPacket Get(int x, int z, short[] positions, int size, IWorldContext world)
    {
        var p = Get<ChunkDeltaUpdateS2CPacket>(PacketId.ChunkDeltaUpdateS2C);
        p.x = x;
        p.z = z;
        p._size = size;
        p.positions = new short[size];
        p.blockRawIds = new byte[size];
        p.blockMetadata = new byte[size];
        Chunk chunk = world.ChunkHost.GetChunk(x, z);

        for (int i = 0; i < size; i++)
        {
            int blockX = positions[i] >> 12 & 15;
            int blockZ = positions[i] >> 8 & 15;
            int blockY = positions[i] & 255;
            p.positions[i] = positions[i];
            p.blockRawIds[i] = (byte)chunk.GetBlockId(blockX, blockY, blockZ);
            p.blockMetadata[i] = (byte)chunk.GetBlockMeta(blockX, blockY, blockZ);
        }

        return p;
    }

    public override void Read(Stream stream)
    {
        x = stream.ReadInt();
        z = stream.ReadInt();
        _size = stream.ReadShort() & '\uffff';
        positions = new short[_size];

        blockRawIds = new byte[_size];
        blockMetadata = new byte[_size];

        for (int i = 0; i < _size; ++i)
        {
            positions[i] = stream.ReadShort();
        }

        stream.ReadExactly(blockRawIds);
        stream.ReadExactly(blockMetadata);
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(x);
        stream.WriteInt(z);
        stream.WriteShort((short)_size);

        for (int i = 0; i < _size; ++i)
        {
            stream.WriteShort(positions[i]);
        }

        stream.Write(blockRawIds);
        stream.Write(blockMetadata);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onChunkDeltaUpdate(this);
    }

    public override int Size()
    {
        return 10 + _size * 4;
    }
}
