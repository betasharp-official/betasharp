using System.Text.RegularExpressions;

namespace BetaSharp.Worlds.Storage;

public static partial class DataFilenameFilter
{
    [GeneratedRegex(@"^c\.(-?[0-9a-z]+)\.(-?[0-9a-z]+)\.dat$", RegexOptions.IgnoreCase)]
    public static partial Regex ChunkFilePattern();
}
