using BetaSharp.Diagnostics;

namespace BetaSharp.Client.Diagnostics;

internal static class ClientMetrics
{
    public static readonly MetricHandle<int> Fps = MetricRegistry.Register<int>("client:fps");
    public static readonly MetricHandle<float> FrameTimeMs = MetricRegistry.Register<float>("client:frame_time_ms");
    public static readonly MetricHandle<long> UploadBytes = MetricRegistry.Register<long>("client:upload_bytes");
    public static readonly MetricHandle<long> DownloadBytes = MetricRegistry.Register<long>("client:download_bytes");
    public static readonly MetricHandle<int> UploadPackets = MetricRegistry.Register<int>("client:upload_packets");
    public static readonly MetricHandle<int> DownloadPackets = MetricRegistry.Register<int>("client:download_packets");
    public static readonly MetricHandle<bool> IsInternal = MetricRegistry.Register<bool>("client:is_internal");
    public static readonly MetricHandle<string> ServerAddress = MetricRegistry.Register<string>("client:server_address");

    static ClientMetrics() { }
}
