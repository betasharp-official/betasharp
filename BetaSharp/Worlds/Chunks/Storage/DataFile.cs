using BetaSharp.Worlds.Storage;

namespace BetaSharp.Worlds.Chunks.Storage;

internal class DataFile : IComparable
{
    private readonly string file;
    private readonly int chunkX;
    private readonly int chunkZ;

    public DataFile(string var1)
    {
        file = var1;
        var match = DataFilenameFilter.ChunkFilePattern().Match(Path.GetFileName(var1));
        if (match.Success)
        {
            chunkX = Convert.ToInt32(match.Groups[1].Value, 36);
            chunkZ = Convert.ToInt32(match.Groups[2].Value, 36);
        }
        else
        {
            chunkX = 0;
            chunkZ = 0;
        }

    }

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
        else
        {
            return var2 - var3;
        }
    }

    public string getFile()
    {
        return file;
    }

    public int GetChunkX()
    {
        return chunkX;
    }

    public int GetChunkZ()
    {
        return chunkZ;
    }

    public int CompareTo(object? var1)
    {
        return comp((DataFile)var1!);
    }
}
