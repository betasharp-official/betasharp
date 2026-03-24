namespace BetaSharp.Worlds.ClientData.Colors;

public class WaterColors
{
    private static int[] waterBuffer = new int[65536];

    public static void loadColors(int[] waterBuffer) => WaterColors.waterBuffer = waterBuffer;
}
