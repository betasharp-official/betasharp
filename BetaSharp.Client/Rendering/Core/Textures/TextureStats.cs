using System.Diagnostics;

namespace BetaSharp.Client.Rendering.Core.Textures;

public static class TextureStats
{
    private static int s_bindsInCurrentFrame;
    private static int s_bindsLastSecond;
    private static int s_frameCountInLastSecond;
    private static readonly Stopwatch s_secondStopwatch = Stopwatch.StartNew();

    public static int BindsLastFrame { get; private set; }
    public static double AverageBindsPerFrame { get; private set; }

    public static void NotifyBind()
    {
        s_bindsInCurrentFrame++;
        s_bindsLastSecond++;
    }

    public static void StartFrame()
    {
        s_bindsInCurrentFrame = 0;
    }

    public static void EndFrame()
    {
        BindsLastFrame = s_bindsInCurrentFrame;
        s_frameCountInLastSecond++;

        if (s_secondStopwatch.ElapsedMilliseconds >= 1000)
        {
            AverageBindsPerFrame =
                s_frameCountInLastSecond > 0 ? s_bindsLastSecond / (double)s_frameCountInLastSecond : 0;
            s_bindsLastSecond = 0;
            s_frameCountInLastSecond = 0;
            s_secondStopwatch.Restart();
        }
    }
}
