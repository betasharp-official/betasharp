using System.Net.Sockets;
using BetaSharp.Worlds;
using java.util.zip;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkDataS2CPacket() : Packet(PacketId.ChunkDataS2C)
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

    public ChunkDataS2CPacket(int x, int y, int z, int sizeX, int sizeY, int sizeZ, World world) : this()
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        byte[] chunkData = world.GetChunkData(x, y, z, sizeX, sizeY, sizeZ);
        rawData = chunkData;
        Deflater deflater = new(1);

        try
        {
            deflater.setInput(chunkData);
            deflater.finish();
            this.chunkData = new byte[sizeX * sizeY * sizeZ * 5 / 2];
            chunkDataSize = deflater.deflate(this.chunkData);
        }
        finally
        {
            deflater.end();
        }
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
        Inflater inflater = new();
        inflater.setInput(chunkData);

        try
        {
            inflater.inflate(this.chunkData);
        }
        catch (DataFormatException ex)
        {
            throw new java.io.IOException("Bad compressed data format");
        }
        finally
        {
            inflater.end();
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
