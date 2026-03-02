using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using BetaSharp.Worlds;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkDataS2CPacket : Packet
{
    public int x;
    public int y;
    public int z;
    public int sizeX;
    public int sizeY;
    public int sizeZ;
    public byte[] chunkData;
    private int chunkDataSize;
    public byte[] rawData;

    public ChunkDataS2CPacket()
    {
        WorldPacket = true;
    }

    public ChunkDataS2CPacket(int x, int y, int z, int sizeX, int sizeY, int sizeZ, World world)
    {
        WorldPacket = true;
        this.x = x;
        this.y = y;
        this.z = z;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        byte[] chunkData = world.GetChunkData(x, y, z, sizeX, sizeY, sizeZ);
        rawData = chunkData;
        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            using (var zlib = new ZLibStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                zlib.Write(chunkData, 0, chunkData.Length);
            compressed = ms.ToArray();
        }
        this.chunkData = compressed;
        chunkDataSize = compressed.Length;
    }

    public override void Read(NetworkStream stream)
    {
        x = stream.ReadInt();
        y = stream.ReadShort();
        z = stream.ReadInt();
        sizeX = stream.ReadByte() + 1;
        sizeY = stream.ReadByte() + 1;
        sizeZ = stream.ReadByte() + 1;
        chunkDataSize = stream.ReadInt();
        byte[] chunkData = new byte[chunkDataSize];
        stream.ReadExactly(chunkData);

        this.chunkData = new byte[sizeX * sizeY * sizeZ * 5 / 2];
        try
        {
            using var ms = new MemoryStream(chunkData);
            using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
            _ = zlib.Read(this.chunkData, 0, this.chunkData.Length);
        }
        catch (InvalidDataException)
        {
            throw new IOException("Bad compressed data format");
        }

    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteInt(x);
        stream.WriteShort((short)y);
        stream.WriteInt(z);
        stream.WriteByte((byte)(sizeX - 1));
        stream.WriteByte((byte)(sizeY - 1));
        stream.WriteByte((byte)(sizeZ - 1));
        stream.WriteInt(chunkDataSize);
        stream.Write(chunkData, 0, chunkDataSize);
    }

    public override void Apply(NetHandler handler)
    {
        handler.handleChunkData(this);
    }

    public override int Size()
    {
        return 17 + chunkDataSize;
    }

    public override void ProcessForInternal()
    {
        chunkData = rawData;
    }
}
