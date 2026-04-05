namespace BetaSharp.Stats;

internal static class StatFormatters
{
    public static string FormatDistance(int value)
    {
        double meters = value / 100.0;
        double kilometers = meters / 1000.0;

        if (kilometers > 0.5) return $"{kilometers:0.##} km";
        if (meters > 0.5) return $"{meters:0.##} m";

        return $"{value} cm";
    }

    public static string FormatTime(int value)
    {
        double seconds = value / 20.0;
        double minutes = seconds / 60.0;
        double hours = minutes / 60.0;
        double days = hours / 24.0;
        double years = days / 365.0;

        return value switch
        {
            _ when years > 0.5 => $"{years:0.##} y",
            _ when days > 0.5 => $"{days:0.##} d",
            _ when hours > 0.5 => $"{hours:0.##} h",
            _ when minutes > 0.5 => $"{minutes:0.##} m",
            _ => $"{seconds} s"
        };
    }
}
