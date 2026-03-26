using BetaSharp.Worlds.Chunks.Storage;

namespace BetaSharp.Worlds.Storage.RegionFormat;

internal class ChunkDataStream(Stream stream, RegionFile.CompressionType compressionType) : IDisposable
{
    public Stream Stream => stream;
    public RegionFile.CompressionType CompressionType => compressionType;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        stream.Dispose();
    }
}
