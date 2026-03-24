using System.IO.Compression;
using System.Net.Sockets;
using BetaSharp.Worlds.Core.Systems;

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

    public static ChunkDataS2CPacket Get(int x, int y, int z, int sizeX, int sizeY, int sizeZ, IWorldContext world)
    {
        var p = Get<ChunkDataS2CPacket>(PacketId.ChunkDataS2C);
        p.x = x;
        p.y = y;
        p.z = z;
        p.sizeX = sizeX;
        p.sizeY = sizeY;
        p.sizeZ = sizeZ;
        p.rawData = world.ChunkHost.GetChunkData(x, y, z, sizeX, sizeY, sizeZ);

        using var output = new MemoryStream(sizeX * sizeY * sizeZ * 5 / 2);
        using var stream = new ZLibStream(output, CompressionLevel.Optimal);

        stream.Write(p.rawData);
        stream.Flush();

        p.chunkData = output.GetBuffer();
        p.chunkDataSize = (int)output.Position;

        return p;
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

        byte[] buffer = new byte[chunkDataSize];
        stream.ReadExactly(buffer);

        using var output = new MemoryStream(sizeX * sizeY * sizeZ * 5 / 2);
        using var decompressor = new ZLibStream(new MemoryStream(buffer), CompressionMode.Decompress);

        decompressor.CopyTo(output);

        chunkData = output.GetBuffer();
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
