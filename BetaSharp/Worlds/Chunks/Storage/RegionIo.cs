using System.IO;

namespace BetaSharp.Worlds.Chunks.Storage;

internal class RegionIo
{
    private static readonly Dictionary<string, WeakReference<RegionFile>> cache = new();
    private static readonly object l = new();

    public static RegionFile func_22193_a(string var0, int var1, int var2)
    {
        lock (l)
        {
            string var3 = Path.Combine(var0, "region");
            string var4 = Path.Combine(var3, "r." + (var1 >> 5) + "." + (var2 >> 5) + ".mcr");
            RegionFile var6;
            if (cache.TryGetValue(var4, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var6!) && var6 != null)
                {
                    return var6;
                }
            }

            if (!Directory.Exists(var3))
            {
                Directory.CreateDirectory(var3);
            }

            if (cache.Count >= 256)
            {
                flush();
            }

            var6 = new RegionFile(var4);
            cache[var4] = new WeakReference<RegionFile>(var6);
            return var6;
        }
    }

    public static void flush()
    {
        lock (l)
        {
            foreach (var weakRef in cache.Values)
            {
                try
                {
                    if (weakRef.TryGetTarget(out RegionFile? var2) && var2 != null)
                    {
                        var2.func_22196_b();
                    }
                }
                catch (IOException)
                {
                }
            }

            cache.Clear();
        }
    }

    public static int getSizeDelta(string var0, int var1, int var2)
    {
        RegionFile var3 = func_22193_a(var0, var1, var2);
        return var3.func_22209_a();
    }

    public static ChunkDataStream GetChunkInputStream(string var0, int var1, int var2)
    {
        RegionFile var3 = func_22193_a(var0, var1, var2);
        return var3.GetChunkDataInputStream(var1 & 31, var2 & 31);
    }

    public static Stream GetChunkOutputStream(string var0, int var1, int var2)
    {
        RegionFile var3 = func_22193_a(var0, var1, var2);
        return var3.GetChunkDataOutputStream(var1 & 31, var2 & 31);
    }
}
