using System.IO.Compression;
using java.io;
using java.util;
using Microsoft.Extensions.Logging;
using Boolean = java.lang.Boolean;
using File = java.io.File;
using IOException = java.io.IOException;

namespace BetaSharp.Worlds.Storage.RegionFormat;

internal class RegionFile
{
    private static readonly byte[] emptySector = new byte[4096];
    private readonly ILogger<RegionFile> _logger = Log.Instance.For<RegionFile>();
    private readonly int[] chunkSaveTimes = new int[1024];
    private readonly RandomAccessFile dataFile;
    private readonly File fileName;
    private readonly int[] offsets = new int[1024];
    private readonly ArrayList sectorFree;
    private int sizeDelta;

    public RegionFile(File var1)
    {
        fileName = var1;
        debugln("REGION LOAD " + fileName);
        sizeDelta = 0;

        try
        {
            dataFile = new RandomAccessFile(var1, "rw");
            int var2;
            if (dataFile.length() < 4096L)
            {
                for (var2 = 0; var2 < 1024; ++var2)
                {
                    dataFile.writeInt(0);
                }

                for (var2 = 0; var2 < 1024; ++var2)
                {
                    dataFile.writeInt(0);
                }

                sizeDelta += 8192;
            }

            if ((dataFile.length() & 4095L) != 0L)
            {
                for (var2 = 0; var2 < (dataFile.length() & 4095L); ++var2)
                {
                    dataFile.write(0);
                }
            }

            var2 = (int)dataFile.length() / 4096;
            sectorFree = new ArrayList(var2);

            int var3;
            for (var3 = 0; var3 < var2; ++var3)
            {
                sectorFree.add(Boolean.TRUE);
            }

            sectorFree.set(0, Boolean.FALSE);
            sectorFree.set(1, Boolean.FALSE);
            dataFile.seek(0L);

            int var4;
            for (var3 = 0; var3 < 1024; ++var3)
            {
                var4 = dataFile.readInt();
                offsets[var3] = var4;
                if (var4 != 0 && (var4 >> 8) + (var4 & 255) <= sectorFree.size())
                {
                    for (int var5 = 0; var5 < (var4 & 255); ++var5)
                    {
                        sectorFree.set((var4 >> 8) + var5, Boolean.FALSE);
                    }
                }
            }

            for (var3 = 0; var3 < 1024; ++var3)
            {
                var4 = dataFile.readInt();
                chunkSaveTimes[var3] = var4;
            }
        }
        catch (IOException ex)
        {
            ex.printStackTrace();
        }
    }

    public int func_22209_a()
    {
        lock (this)
        {
            int var1 = sizeDelta;
            sizeDelta = 0;
            return var1;
        }
    }

    private void func_22211_a(string var1)
    {
    }

    private void debugln(string var1) => func_22211_a(var1 + "\n");

    private void func_22199_a(string var1, int var2, int var3, string var4) => func_22211_a("REGION " + var1 + " " + fileName.getName() + "[" + var2 + "," + var3 + "] = " + var4);

    private void func_22197_a(string var1, int var2, int var3, int var4, string var5) => func_22211_a("REGION " + var1 + " " + fileName.getName() + "[" + var2 + "," + var3 + "] " + var4 + "B = " + var5);

    private void debugln(string var1, int var2, int var3, string var4) => func_22199_a(var1, var2, var3, var4 + "\n");

    public ChunkDataStream GetChunkDataInputStream(int var1, int var2)
    {
        lock (this)
        {
            if (outOfBounds(var1, var2))
            {
                debugln("READ", var1, var2, "out of bounds");
                return null;
            }

            try
            {
                int var3 = getOffset(var1, var2);
                if (var3 == 0)
                {
                    return null;
                }

                int var4 = var3 >> 8;
                int var5 = var3 & 255;
                if (var4 + var5 > sectorFree.size())
                {
                    debugln("READ", var1, var2, "invalid sector");
                    return null;
                }

                dataFile.seek(var4 * 4096);
                int var6 = dataFile.readInt();
                if (var6 > 4096 * var5)
                {
                    debugln("READ", var1, var2, "invalid length: " + var6 + " > 4096 * " + var5);
                    return null;
                }

                CompressionType var7 = (CompressionType)dataFile.readByte();
                byte[] var8;
                Stream var9;

                if (var7 == CompressionType.ZLibDeflate)
                {
                    var8 = new byte[var6 - 1];
                    dataFile.read(var8);
                    var9 = new ZLibStream(new MemoryStream(var8), CompressionMode.Decompress);
                    return new ChunkDataStream(var9, var7);
                }

                debugln("READ", var1, var2, "unknown version " + var7);
                return null;
            }
            catch (System.IO.IOException)
            {
                debugln("READ", var1, var2, "exception");
                return null;
            }
        }
    }

    public Stream GetChunkDataOutputStream(int var1, int var2)
    {
        if (outOfBounds(var1, var2))
        {
            return null;
        }

        RegionFileChunkBuffer buffer = new(this, var1, var2);
        return new ZLibStream(buffer, CompressionMode.Compress);
    }

    public void write(int var1, int var2, byte[] var3, int var4)
    {
        lock (this)
        {
            try
            {
                int var5 = getOffset(var1, var2);
                int var6 = var5 >> 8;
                int var7 = var5 & 255;
                int var8 = (var4 + 5) / 4096 + 1;
                if (var8 >= 256)
                {
                    return;
                }

                if (var6 != 0 && var7 == var8)
                {
                    func_22197_a("SAVE", var1, var2, var4, "rewrite");
                    write(var6, var3, var4);
                }
                else
                {
                    int var9;
                    for (var9 = 0; var9 < var7; ++var9)
                    {
                        sectorFree.set(var6 + var9, Boolean.TRUE);
                    }

                    var9 = sectorFree.indexOf(Boolean.TRUE);
                    int var10 = 0;
                    int var11;
                    if (var9 != -1)
                    {
                        for (var11 = var9; var11 < sectorFree.size(); ++var11)
                        {
                            if (var10 != 0)
                            {
                                if (((Boolean)sectorFree.get(var11)).booleanValue())
                                {
                                    ++var10;
                                }
                                else
                                {
                                    var10 = 0;
                                }
                            }
                            else if (((Boolean)sectorFree.get(var11)).booleanValue())
                            {
                                var9 = var11;
                                var10 = 1;
                            }

                            if (var10 >= var8)
                            {
                                break;
                            }
                        }
                    }

                    if (var10 >= var8)
                    {
                        func_22197_a("SAVE", var1, var2, var4, "reuse");
                        var6 = var9;
                        setOffset(var1, var2, (var9 << 8) | var8);

                        for (var11 = 0; var11 < var8; ++var11)
                        {
                            sectorFree.set(var6 + var11, Boolean.FALSE);
                        }

                        write(var6, var3, var4);
                    }
                    else
                    {
                        func_22197_a("SAVE", var1, var2, var4, "grow");
                        dataFile.seek(dataFile.length());
                        var6 = sectorFree.size();

                        for (var11 = 0; var11 < var8; ++var11)
                        {
                            dataFile.write(emptySector);
                            sectorFree.add(Boolean.FALSE);
                        }

                        sizeDelta += 4096 * var8;
                        write(var6, var3, var4);
                        setOffset(var1, var2, (var6 << 8) | var8);
                    }
                }

                func_22208_b(var1, var2, (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                               / 1000L));
            }
            catch (System.IO.IOException var12)
            {
                _logger.LogError(var12, "Exception");
            }
        }
    }

    private void write(int var1, byte[] var2, int var3)
    {
        debugln(" " + var1);
        dataFile.seek(var1 * 4096);
        dataFile.writeInt(var3 + 1);
        dataFile.writeByte((byte)CompressionType.ZLibDeflate);
        dataFile.write(var2, 0, var3);
    }

    private bool outOfBounds(int var1, int var2) => var1 < 0 || var1 >= 32 || var2 < 0 || var2 >= 32;

    private int getOffset(int var1, int var2) => offsets[var1 + var2 * 32];

    public bool func_22202_c(int var1, int var2) => getOffset(var1, var2) != 0;

    private void setOffset(int var1, int var2, int var3)
    {
        offsets[var1 + var2 * 32] = var3;
        dataFile.seek((var1 + var2 * 32) * 4);
        dataFile.writeInt(var3);
    }

    private void func_22208_b(int var1, int var2, int var3)
    {
        chunkSaveTimes[var1 + var2 * 32] = var3;
        dataFile.seek(4096 + (var1 + var2 * 32) * 4);
        dataFile.writeInt(var3);
    }

    public void func_22196_b() => dataFile.close();

    internal enum CompressionType : byte
    {
        GZipUnused = 1,
        ZLibDeflate,
        OldRegionUnused
    }
}
