namespace BetaSharp.Client.UI.Layout.Flexbox;

class Constant
{
    internal const int EdgeCount = 9;
    internal const int ExperimentalFeatureCount = 1;
    internal const int MeasureModeCount = 3;

    /// <summary>
    /// This value was chosen based on empiracle data. Even the most complicated
    /// layouts should not require more than 16 entries to fit within the cache.
    /// </summary>
    internal const int MaxCachedResultCount = 16;

    internal const int measureModeCount = 3;
    internal static readonly string[] measureModeNames = { "UNDEFINED", "EXACTLY", "AT_MOST" };
    internal static readonly string[] layoutModeNames = { "LAY_UNDEFINED", "LAY_EXACTLY", "LAY_AT_MOST" };



    internal readonly static Node nodeDefaults = Flex.CreateDefaultNode();
    internal readonly static Config configDefaults = Flex.CreateDefaultConfig();



    internal const float defaultFlexGrow = 0;
    internal const float defaultFlexShrink = 0;
    internal const float webDefaultFlexShrink = 1;
}
