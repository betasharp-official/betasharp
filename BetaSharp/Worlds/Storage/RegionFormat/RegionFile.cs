using System.IO.Compression;
using BetaSharp.Worlds.Chunks.Storage;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Worlds.Storage.RegionFormat;

internal class RegionFile
{
    private static readonly byte[] s_emptySector = new byte[4096];
    private readonly ILogger<RegionFile> _logger = Log.Instance.For<RegionFile>();
    private readonly int[] _chunkSaveTimes = new int[1024];
    private readonly FileStream _dataFile;
    private readonly int[] _offsets = new int[1024];
    private readonly List<bool> _sectorFree;
    private int _sizeDelta;

    public RegionFile(string input)
    {
        _sizeDelta = 0;

        try
        {
            _dataFile = new FileStream(input, FileMode.OpenOrCreate);

            int headerIndex;
            if (_dataFile.Length < 4096L)
            {
                for (headerIndex = 0; headerIndex < 1024; ++headerIndex)
                {
                    _dataFile.WriteInt(0);
                }

                for (headerIndex = 0; headerIndex < 1024; ++headerIndex)
                {
                    _dataFile.WriteInt(0);
                }

                _sizeDelta += 8192;
            }

            if ((_dataFile.Length & 4095L) != 0L)
            {
                for (headerIndex = 0; headerIndex < (_dataFile.Length & 4095L); ++headerIndex)
                {
                    _dataFile.WriteByte(0);
                }
            }

            headerIndex = (int)_dataFile.Length / 4096;
            _sectorFree = new List<bool>(headerIndex);

            int sectorIndex;
            for (sectorIndex = 0; sectorIndex < headerIndex; ++sectorIndex)
            {
                _sectorFree.Add(true);
            }

            _sectorFree[0] = false;
            _sectorFree[1] = false;
            _dataFile.Seek(0L, SeekOrigin.Begin);

            int offset;
            for (sectorIndex = 0; sectorIndex < 1024; ++sectorIndex)
            {
                offset = _dataFile.ReadInt();
                _offsets[sectorIndex] = offset;
                if (offset != 0 && (offset >> 8) + (offset & 255) <= _sectorFree.Count())
                {
                    for (int usedSectorIndex = 0; usedSectorIndex < (offset & 255); ++usedSectorIndex)
                    {
                        _sectorFree[(offset >> 8) + usedSectorIndex] = false;
                    }
                }
            }

            for (sectorIndex = 0; sectorIndex < 1024; ++sectorIndex)
            {
                offset = _dataFile.ReadInt();
                _chunkSaveTimes[sectorIndex] = offset;
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex);
        }
    }

    public int func_22209_a()
    {
        lock (this)
        {
            int delta = _sizeDelta;
            _sizeDelta = 0;
            return delta;
        }
    }

    public ChunkDataStream GetChunkDataInputStream(int chunkX, int chunkZ)
    {
        lock (this)
        {
            if (OutOfBounds(chunkX, chunkZ))
            {
                return null;
            }

            try
            {
                int offset = GetOffset(chunkX, chunkZ);
                if (offset == 0)
                {
                    return null;
                }

                int sectorNumber = offset >> 8;
                int sectorCount = offset & 255;

                if (sectorNumber + sectorCount > _sectorFree.Count())
                {
                    return null;
                }

                _dataFile.Seek(sectorNumber * 4096, SeekOrigin.Begin);
                int compressedLength = _dataFile.ReadInt();
                if (compressedLength > 4096 * sectorCount)
                {
                    return null;
                }

                CompressionType compressionType = (CompressionType)_dataFile.ReadByte();
                byte[] compressedData;
                Stream stream;

                if (compressionType == CompressionType.ZLibDeflate)
                {
                    compressedData = new byte[compressedLength - 1];
                    _dataFile.ReadExactly(compressedData);
                    stream = new ZLibStream(new MemoryStream(compressedData), CompressionMode.Decompress);
                    return new ChunkDataStream(stream, compressionType);
                }

                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }

    public Stream GetChunkDataOutputStream(int chunkX, int chunkZ)
    {
        if (OutOfBounds(chunkX, chunkZ))
        {
            return null;
        }

        RegionFileChunkBuffer buffer = new(this, chunkX, chunkZ);
        return new ZLibStream(buffer, CompressionMode.Compress);
    }

    public void Write(int chunkX, int chunkZ, byte[] data, int length)
    {
        lock (this)
        {
            try
            {
                int offset = GetOffset(chunkX, chunkZ);
                int sectorNumber = offset >> 8;
                int allocatedSectorCount = offset & 255;
                int requiredSectorCount = (length + 5) / 4096 + 1;
                if (requiredSectorCount >= 256)
                {
                    return;
                }

                if (sectorNumber != 0 && allocatedSectorCount == requiredSectorCount)
                {
                    Write(sectorNumber, data, length);
                }
                else
                {
                    int freeRunStart;
                    for (freeRunStart = 0; freeRunStart < allocatedSectorCount; ++freeRunStart)
                    {
                        _sectorFree[sectorNumber + freeRunStart] = true;
                    }

                    freeRunStart = _sectorFree.IndexOf(true);
                    int freeRunLength = 0;
                    int sectorIndex;
                    if (freeRunStart != -1)
                    {
                        for (sectorIndex = freeRunStart; sectorIndex < _sectorFree.Count(); ++sectorIndex)
                        {
                            if (freeRunLength != 0)
                            {
                                if (_sectorFree[sectorIndex])
                                {
                                    ++freeRunLength;
                                }
                                else
                                {
                                    freeRunLength = 0;
                                }
                            }
                            else if (_sectorFree[sectorIndex])
                            {
                                freeRunStart = sectorIndex;
                                freeRunLength = 1;
                            }

                            if (freeRunLength >= requiredSectorCount)
                            {
                                break;
                            }
                        }
                    }

                    if (freeRunLength >= requiredSectorCount)
                    {
                        sectorNumber = freeRunStart;
                        SetOffset(chunkX, chunkZ, (freeRunStart << 8) | requiredSectorCount);

                        for (sectorIndex = 0; sectorIndex < requiredSectorCount; ++sectorIndex)
                        {
                            _sectorFree[sectorNumber + sectorIndex] = false;
                        }

                        Write(sectorNumber, data, length);
                    }
                    else
                    {
                        _dataFile.Seek(_dataFile.Length, SeekOrigin.Begin);
                        sectorNumber = _sectorFree.Count();

                        for (sectorIndex = 0; sectorIndex < requiredSectorCount; ++sectorIndex)
                        {
                            _dataFile.Write(s_emptySector);
                            _sectorFree.Add(false);
                        }

                        _sizeDelta += 4096 * requiredSectorCount;
                        Write(sectorNumber, data, length);
                        SetOffset(chunkX, chunkZ, (sectorNumber << 8) | requiredSectorCount);
                    }
                }

                func_22208_b(chunkX, chunkZ, (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000L));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Exception");
            }
        }
    }

    private void Write(int sectorNumber, byte[] data, int length)
    {
        _dataFile.Seek(sectorNumber * 4096, SeekOrigin.Begin);
        _dataFile.WriteInt(length + 1);
        _dataFile.WriteByte((byte)CompressionType.ZLibDeflate);
        _dataFile.Write(data, 0, length);
    }

    private static bool OutOfBounds(int chunkX, int chunkZ) => chunkX < 0 || chunkX >= 32 || chunkZ < 0 || chunkZ >= 32;

    private int GetOffset(int chunkX, int chunkZ) => _offsets[chunkX + chunkZ * 32];

    private void SetOffset(int chunkX, int chunkZ, int offset)
    {
        _offsets[chunkX + chunkZ * 32] = offset;
        _dataFile.Seek((chunkX + chunkZ * 32) * 4, SeekOrigin.Begin);
        _dataFile.WriteInt(offset);
    }

    private void func_22208_b(int chunkX, int chunkZ, int timestamp)
    {
        _chunkSaveTimes[chunkX + chunkZ * 32] = timestamp;
        _dataFile.Seek(4096 + (chunkX + chunkZ * 32) * 4, SeekOrigin.Begin);
        _dataFile.WriteInt(timestamp);
    }

    public void Flush()
    {
        _dataFile.Flush();
        _dataFile.Dispose();
    }

    internal enum CompressionType : byte
    {
        GZipUnused = 1,
        ZLibDeflate,
        OldRegionUnused
    }
}
