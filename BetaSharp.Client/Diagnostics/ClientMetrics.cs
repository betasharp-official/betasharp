using BetaSharp.Diagnostics;

namespace BetaSharp.Client.Diagnostics;

internal static class ClientMetrics
{
    public static readonly MetricHandle<int>   Fps         = MetricRegistry.Register<int>("client:fps");
    public static readonly MetricHandle<float> FrameTimeMs = MetricRegistry.Register<float>("client:frame_time_ms");

    static ClientMetrics() { }
}
