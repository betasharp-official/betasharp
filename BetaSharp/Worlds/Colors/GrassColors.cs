namespace BetaSharp.Worlds.Colors;

public class GrassColors
{
    private static int[] grassBuffer = new int[65536];

    public static void loadColors(int[] grassBuffer)
    {
        GrassColors.grassBuffer = grassBuffer;
    }

    public static int getColor(double temperature, double downfall)
    {
        downfall *= temperature;
        int var4 = (int)((1.0D - temperature) * 255.0D);
        int var5 = (int)((1.0D - downfall) * 255.0D);
        return grassBuffer[var5 << 8 | var4];
    }

    public static int getDefaultColor()
    {
        return 0x79C05A;
    }
}
