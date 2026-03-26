using System.Text.RegularExpressions;

namespace BetaSharp.Worlds.Storage;

public partial class DimensionFileFilter
{
    [GeneratedRegex(@"^[0-9a-z]{1,2}$", RegexOptions.IgnoreCase)]
    public static partial Regex DimensionPattern();
}
