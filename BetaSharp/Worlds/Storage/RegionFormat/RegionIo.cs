using java.lang.@ref;
using java.util;
using File = java.io.File;
using IOException = java.io.IOException;

namespace BetaSharp.Worlds.Storage.RegionFormat;

internal class RegionIo
{
    private static readonly Map cache = new HashMap();
    private static readonly object l = new();

    public static RegionFile func_22193_a(File var0, int var1, int var2)
    {
        lock (l)
        {
            File var3 = new(var0, "region");
            File var4 = new(var3, "r." + (var1 >> 5) + "." + (var2 >> 5) + ".mcr");
            Reference var5 = (Reference)cache.get(var4);
            RegionFile var6;
            if (var5 != null)
            {
                var6 = (RegionFile)var5.get();
                if (var6 != null)
                {
                    return var6;
                }
            }

            if (!var3.exists())
            {
                var3.mkdirs();
            }

            if (cache.size() >= 256)
            {
                flush();
            }

            var6 = new RegionFile(var4);
            cache.put(var4, new SoftReference(var6));
            return var6;
        }
    }

    public static void flush()
    {
        lock (l)
        {
            Iterator var0 = cache.values().iterator();

            while (var0.hasNext())
            {
                Reference var1 = (Reference)var0.next();

                try
                {
                    RegionFile var2 = (RegionFile)var1.get();
                    if (var2 != null)
                    {
                        var2.func_22196_b();
                    }
                }
                catch (IOException ex)
                {
                    ex.printStackTrace();
                }
            }

            cache.clear();
        }
    }

    public static int getSizeDelta(File var0, int var1, int var2)
    {
        RegionFile var3 = func_22193_a(var0, var1, var2);
        return var3.func_22209_a();
    }

    public static ChunkDataStream GetChunkInputStream(File var0, int var1, int var2)
    {
        RegionFile var3 = func_22193_a(var0, var1, var2);
        return var3.GetChunkDataInputStream(var1 & 31, var2 & 31);
    }

    public static Stream GetChunkOutputStream(File var0, int var1, int var2)
    {
        RegionFile var3 = func_22193_a(var0, var1, var2);
        return var3.GetChunkDataOutputStream(var1 & 31, var2 & 31);
    }
}
