namespace BetaSharp.Diagnostics;

/// <summary>
/// Metrics for the server.
/// </summary>
public static class ServerMetrics
{
    public static readonly MetricHandle<float> Tps = MetricRegistry.Register<float>("server:tps");

    /// <summary>
    /// Amount of milliseconds each tick takes, in float.
    /// </summary>
    public static readonly MetricHandle<float> Mspt = MetricRegistry.Register<float>("server:mspt");

    public static readonly MetricHandle<int> EntityCount = MetricRegistry.Register<int>("server:entity_count");
    public static readonly MetricHandle<int> PlayerCount = MetricRegistry.Register<int>("server:player_count");

    static ServerMetrics() { }
}
