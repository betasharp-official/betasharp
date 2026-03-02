using BetaSharp.Entities;

namespace BetaSharp;

public class MapInfo
{
    public readonly EntityPlayer player;
    public int[] startZ;
    public int[] endZ;
    private int nextDirtyPixel;
    private int colorsUpdateInterval;
    readonly MapState mapDataObj;
    private byte[] iconsData;

    public MapInfo(MapState var1, EntityPlayer var2)
    {
        mapDataObj = var1;
        startZ = new int[128];
        endZ = new int[128];
        nextDirtyPixel = 0;
        colorsUpdateInterval = 0;
        player = var2;

        for (int var3 = 0; var3 < startZ.Length; ++var3)
        {
            startZ[var3] = 0;
            endZ[var3] = 127;
        }

    }

    public byte[] getUpdateData()
    {
        if (--colorsUpdateInterval < 0)
        {
            colorsUpdateInterval = 4;
            byte[] var2 = new byte[mapDataObj.icons.Count * 3 + 1];
            var2[0] = 1;

            for (int var3 = 0; var3 < mapDataObj.icons.Count; var3++)
            {
                MapCoord var4 = mapDataObj.icons[var3];
                var2[var3 * 3 + 1] = (byte)(var4.type + (var4.rotation & 15) * 16);
                var2[var3 * 3 + 2] = var4.x;
                var2[var3 * 3 + 3] = var4.z;
            }

            bool var9 = true;
            if (iconsData != null && iconsData.Length == var2.Length)
            {
                for (int var11 = 0; var11 < var2.Length; var11++)
                {
                    if (var2[var11] != iconsData[var11])
                    {
                        var9 = false;
                        break;
                    }
                }
            }
            else
            {
                var9 = false;
            }

            if (!var9)
            {
                iconsData = var2;
                return var2;
            }
        }

        for (int var8 = 0; var8 < 10; var8++)
        {
            int var10 = nextDirtyPixel * 11 % 128;
            nextDirtyPixel++;
            if (startZ[var10] >= 0)
            {
                int var12 = endZ[var10] - startZ[var10] + 1;
                int var5 = startZ[var10];
                byte[] var6 = new byte[var12 + 3];
                var6[0] = 0;
                var6[1] = (byte)var10;
                var6[2] = (byte)var5;

                for (int var7 = 0; var7 < var6.Length - 3; var7++)
                {
                    var6[var7 + 3] = mapDataObj.colors[(var7 + var5) * 128 + var10];
                }

                endZ[var10] = -1;
                startZ[var10] = -1;
                return var6;
            }
        }

        return null;
    }
}
