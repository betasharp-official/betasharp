using BetaSharp.Client.Options;

namespace BetaSharp.Client;

public class ScaledResolution
{
    public int ScaledWidth { get; private set; }
    public int ScaledHeight { get; private set; }
    public double ScaledWidthDouble { get; private set; }
    public double ScaledHeightDouble { get; private set; }
    public int ScaleFactor { get; private set; }

    public ScaledResolution(GameOptions options, int scaledWidth, int scaledHeight)
    {
        ScaledWidth = scaledWidth;
        ScaledHeight = scaledHeight;
        int guiScale = options.GuiScale;
        ScaleFactor = 1;

        if (guiScale == 0)
            guiScale = 1000;

        while (ScaleFactor < guiScale && ScaledWidth / (ScaleFactor + 1) >= 320 && ScaledHeight / (ScaleFactor + 1) >= 240)
        {
            ++ScaleFactor;
        }

        ScaledWidthDouble = ScaledWidth / (double)ScaleFactor;
        ScaledHeightDouble = ScaledHeight / (double)ScaleFactor;
        ScaledWidth = (int)Math.Ceiling(ScaledWidthDouble);
        ScaledHeight = (int)Math.Ceiling(ScaledHeightDouble);
    }
}
