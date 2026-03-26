namespace BetaSharp.Worlds.Colors;

public class FoliageColors
{
    private static int[] foliageBuffer = new int[65536];

    public static void loadColors(int[] foliageBuffer)
    {
        FoliageColors.foliageBuffer = foliageBuffer;
    }

    public static int getFoliageColor(double temperature, double downfall)
    {
        downfall *= temperature;
        int temperatureIndex = (int)((1.0D - temperature) * 255.0D);
        int downfallIndex = (int)((1.0D - downfall) * 255.0D);
        return foliageBuffer[downfallIndex << 8 | temperatureIndex];
    }

    public static int getSpruceColor()
    {
        return 0x619961;
    }

    public static int getBirchColor()
    {
        return 0x80A755;
    }

    public static int getDefaultColor()
    {
        return 0x48B518;
    }
}