namespace BetaSharp.Worlds.ClientData.Colors;

public class GrassColors
{
    private static int[] grassBuffer = new int[65536];

    public static void loadColors(int[] grassBuffer) => GrassColors.grassBuffer = grassBuffer;

    public static int getColor(double temperature, double downfall)
    {
        downfall *= temperature;
        int temperatureIndex = (int)((1.0D - temperature) * 255.0D);
        int downfallIndex = (int)((1.0D - downfall) * 255.0D);
        return grassBuffer[downfallIndex << 8 | temperatureIndex];
    }

    public static int getDefaultColor() => 0x79C05A;
}
