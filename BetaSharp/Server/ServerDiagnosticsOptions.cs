using BetaSharp.Profiling;

namespace BetaSharp.Server;

public sealed class ServerDiagnosticsOptions
{
    public required string RequestedProfilingDetailRaw { get; init; }
    public required string RequestedProfilingDetail { get; init; }
    public required ProfilingDetailLevel ProfilingDetail { get; init; }
    public required bool ProfilingDetailCappedToBuild { get; init; }
    public required bool ProfilingDetailValueRecognized { get; init; }
    public required bool MetricsHttpEnabled { get; init; }
    public required string MetricsHttpHost { get; init; }
    public required int MetricsHttpPort { get; init; }
    public required int StatsLogIntervalSeconds { get; init; }

    public static ServerDiagnosticsOptions FromConfiguration(IServerConfiguration config)
    {
        string requestedRaw = config.GetProfilingDetail(GetDefaultProfilingDetail());
        (ProfilingDetailLevel level, string requestedCanonical, bool recognized, bool cappedToBuild) = ParseProfilingDetail(requestedRaw);

        return new ServerDiagnosticsOptions
        {
            RequestedProfilingDetailRaw = requestedRaw,
            RequestedProfilingDetail = requestedCanonical,
            ProfilingDetail = level,
            ProfilingDetailCappedToBuild = cappedToBuild,
            ProfilingDetailValueRecognized = recognized,
            MetricsHttpEnabled = config.GetMetricsHttpEnabled(false),
            MetricsHttpHost = config.GetMetricsHttpHost("0.0.0.0"),
            MetricsHttpPort = config.GetMetricsHttpPort(9464),
            StatsLogIntervalSeconds = Math.Max(0, config.GetStatsLogIntervalSeconds(0)),
        };
    }

    private static string GetDefaultProfilingDetail()
    {
#if DEBUG
        return "detailed";
#else
        return "basic";
#endif
    }

    private static (ProfilingDetailLevel Level, string RequestedCanonical, bool Recognized, bool CappedToBuild) ParseProfilingDetail(string value)
    {
        string normalized = value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "off" or "disabled" or "none" => (ProfilingDetailLevel.Disabled, "disabled", true, false),
            "basic" => (ProfilingDetailLevel.Basic, "basic", true, false),
            "detailed" or "deep" => BuildDetailedResult(),
            _ => (ProfilingDetailLevel.Basic, "basic", false, false),
        };
    }

    private static (ProfilingDetailLevel Level, string RequestedCanonical, bool Recognized, bool CappedToBuild) BuildDetailedResult()
    {
        ProfilingDetailLevel level = GetMaxProfilingDetail();
        return (level, "detailed", true, level != ProfilingDetailLevel.Detailed);
    }

    private static ProfilingDetailLevel GetMaxProfilingDetail()
    {
#if DEBUG
        return ProfilingDetailLevel.Detailed;
#else
        return ProfilingDetailLevel.Basic;
#endif
    }
}
