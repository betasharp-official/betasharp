namespace BetaSharp.Worlds.ClientData.Colors;

public class FoliageColors
{
    private static int[] foliageBuffer = new int[65536];

    public static void loadColors(int[] foliageBuffer) => FoliageColors.foliageBuffer = foliageBuffer;

    public static int getFoliageColor(double temperature, double downfall)
    {
        downfall *= temperature;
        int var4 = (int)((1.0D - temperature) * 255.0D);
        int var5 = (int)((1.0D - downfall) * 255.0D);
        return foliageBuffer[(var5 << 8) | var4];
    }

    public static int getSpruceColor() => 0x619961;

    public static int getBirchColor() => 0x80A755;

    public static int getDefaultColor() => 0x48B518;
}
