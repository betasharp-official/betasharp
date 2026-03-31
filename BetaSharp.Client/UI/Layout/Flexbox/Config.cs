namespace BetaSharp.Client.UI.Layout.Flexbox;

public class Config
{
    public bool UseWebDefaults = false;
    public object Context = null;
    public LoggerFunc Logger = DefaultLog;

    readonly internal bool[] experimentalFeatures = new bool[Constant.ExperimentalFeatureCount + 1];
    internal bool UseLegacyStretchBehaviour = false;
    internal float PointScaleFactor = 1;

    public static int DefaultLog(Config config, Node node, LogLevel level, string format, params object[] args)
    {
        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Fatal:
                System.Console.WriteLine(format, args);
                return 0;
            case LogLevel.Warn:
            case LogLevel.Info:
            case LogLevel.Debug:
            case LogLevel.Verbose:
            default:
                System.Console.WriteLine(format, args);
                break;
        }

        return 0;
    }

    public static void Copy(Config dest, Config src)
    {
        dest.UseWebDefaults = src.UseWebDefaults;
        dest.UseLegacyStretchBehaviour = src.UseLegacyStretchBehaviour;
        dest.PointScaleFactor = src.PointScaleFactor;
        dest.Logger = src.Logger;
        dest.Context = src.Context;

        for (int i = 0; i < src.experimentalFeatures.Length; i++)
        {
            dest.experimentalFeatures[i] = src.experimentalFeatures[i];
        }
    }

    // SetExperimentalFeatureEnabled enables experimental feature
    public void SetExperimentalFeatureEnabled(ExperimentalFeature feature, bool enabled)
    {
        this.experimentalFeatures[(int)feature] = enabled;
    }

    // IsExperimentalFeatureEnabled returns if experimental feature is enabled
    public bool IsExperimentalFeatureEnabled(ExperimentalFeature feature)
    {
        return this.experimentalFeatures[(int)feature];
    }


    // SetPointScaleFactor sets scale factor
    public void SetPointScaleFactor(float pixelsInPoint)
    {
        assertWithConfig(this, pixelsInPoint >= 0, "Scale factor should not be less than zero");

        // We store points for Pixel as we will use it for rounding
        if (pixelsInPoint == 0)
        {
            // Zero is used to skip rounding
            this.PointScaleFactor = 0;
        }
        else
        {
            this.PointScaleFactor = pixelsInPoint;
        }
    }

    internal static void assertWithConfig(Config config, bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
