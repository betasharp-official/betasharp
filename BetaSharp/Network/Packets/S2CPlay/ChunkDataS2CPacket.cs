using System.IO.Compression;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp.Network.Packets.S2CPlay;

public class ChunkDataS2CPacket() : Packet(PacketId.ChunkDataS2C)
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Z { get; private set; }
    public int SizeX { get; private set; }
    public int SizeY { get; private set; }
    public int SizeZ { get; private set; }
    public byte[] ChunkData { get; private set; } = Array.Empty<byte>();
    private int ChunkDataSize { get; set; }
    private byte[] RawData { get; set; } = Array.Empty<byte>();

    public static ChunkDataS2CPacket Get(int x, int y, int z, int sizeX, int sizeY, int sizeZ, IWorldContext world)
    {
        ChunkDataS2CPacket p = Get<ChunkDataS2CPacket>(PacketId.ChunkDataS2C);
        p.X = x;
        p.Y = y;
        p.Z = z;
        p.SizeX = sizeX;
        p.SizeY = sizeY;
        p.SizeZ = sizeZ;
        p.RawData = world.ChunkHost.GetChunkData(x, y, z, sizeX, sizeY, sizeZ);

        using MemoryStream output = new(sizeX * sizeY * sizeZ * 5 / 2);
        using ZLibStream stream = new(output, CompressionLevel.Optimal);

        stream.Write(p.RawData);
        stream.Flush();

        p.ChunkData = output.GetBuffer();
        p.ChunkDataSize = (int)output.Position;

        return p;
    }

    public override void Read(Stream stream)
    {
        X = stream.ReadInt();
        Y = stream.ReadShort();
        Z = stream.ReadInt();
        SizeX = stream.ReadByte() + 1;
        SizeY = stream.ReadByte() + 1;
        SizeZ = stream.ReadByte() + 1;
        ChunkDataSize = stream.ReadInt();

        byte[] buffer = new byte[ChunkDataSize];
        stream.ReadExactly(buffer);

        using MemoryStream output = new(SizeX * SizeY * SizeZ * 5 / 2);
        using ZLibStream decompressor = new(new MemoryStream(buffer), CompressionMode.Decompress);

        decompressor.CopyTo(output);

        ChunkData = output.GetBuffer();
    }

    public override void Write(Stream stream)
    {
        stream.WriteInt(X);
        stream.WriteShort((short)Y);
        stream.WriteInt(Z);
        stream.WriteByte((byte)(SizeX - 1));
        stream.WriteByte((byte)(SizeY - 1));
        stream.WriteByte((byte)(SizeZ - 1));
        stream.WriteInt(ChunkDataSize);
        stream.Write(ChunkData, 0, ChunkDataSize);
    }

    public override void Apply(NetHandler handler) => handler.handleChunkData(this);

    public override int Size() => 17 + ChunkDataSize;

    public override void ProcessForInternal() => ChunkData = RawData;
}
