using System.Text.RegularExpressions;
using java.lang;
using File = java.io.File;

namespace BetaSharp.Worlds.Storage.RegionFormat;

internal class DataFile : Comparable
{
    private readonly int chunkX;
    private readonly int chunkZ;
    private readonly File file;

    public DataFile(File var1)
    {
        file = var1;
        Match match = DataFilenameFilter.ChunkFilePattern().Match(var1.getName());
        if (match.Success)
        {
            chunkX = Integer.parseInt(match.Groups[1].Value, 36);
            chunkZ = Integer.parseInt(match.Groups[2].Value, 36);
        }
        else
        {
            chunkX = 0;
            chunkZ = 0;
        }
    }

    public int CompareTo(object? var1) => comp((DataFile)var1!);

    public int comp(DataFile var1)
    {
        int var2 = chunkX >> 5;
        int var3 = var1.chunkX >> 5;
        if (var2 == var3)
        {
            int var4 = chunkZ >> 5;
            int var5 = var1.chunkZ >> 5;
            return var4 - var5;
        }

        return var2 - var3;
    }

    public File getFile() => file;

    public int GetChunkX() => chunkX;

    public int GetChunkZ() => chunkZ;
}
