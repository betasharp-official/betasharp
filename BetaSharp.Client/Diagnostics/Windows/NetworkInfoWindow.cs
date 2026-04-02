using BetaSharp.Diagnostics;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class NetworkInfoWindow : DebugWindow
{
    private readonly FrameGraph _uploadGraph;
    private readonly FrameGraph _downloadGraph;

    private long _lastUploadBytes;
    private long _lastDownloadBytes;

    private readonly Queue<(float Time, long Upload, long Download)> _history = new();
    private float _currentTime;

    public override string Title => "Network Info";

    public NetworkInfoWindow()
    {
        _uploadGraph = new FrameGraph("Upload (B/s)", 240);
        _downloadGraph = new FrameGraph("Download (B/s)", 240);
    }

    protected override void OnDraw()
    {
        bool isInternal = MetricRegistry.Get(ClientMetrics.IsInternal);
        string serverAddress = MetricRegistry.Get(ClientMetrics.ServerAddress) ?? "Unknown";
        long currentUpload = MetricRegistry.Get(ClientMetrics.UploadBytes);
        long currentDownload = MetricRegistry.Get(ClientMetrics.DownloadBytes);
        int uploadPackets = MetricRegistry.Get(ClientMetrics.UploadPackets);
        int downloadPackets = MetricRegistry.Get(ClientMetrics.DownloadPackets);

        long uploadDelta = currentUpload - _lastUploadBytes;
        long downloadDelta = currentDownload - _lastDownloadBytes;
        if (uploadDelta < 0) uploadDelta = 0;
        if (downloadDelta < 0) downloadDelta = 0;

        _currentTime += ImGui.GetIO().DeltaTime;
        _history.Enqueue((_currentTime, uploadDelta, downloadDelta));

        long sumUpload = 0;
        long sumDownload = 0;

        while (_history.Count > 0 && _currentTime - _history.Peek().Time > 1.0f)
        {
            _history.Dequeue();
        }

        foreach (var entry in _history)
        {
            sumUpload += entry.Upload;
            sumDownload += entry.Download;
        }

        _uploadGraph.Push(sumUpload);
        _downloadGraph.Push(sumDownload);

        _lastUploadBytes = currentUpload;
        _lastDownloadBytes = currentDownload;

        if (ImGui.CollapsingHeader("Connection statistics", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Connection Type: {(isInternal ? "Internal Server" : "Remote Server")}");
            if (!isInternal)
            {
                ImGui.Text($"Address: {serverAddress}");
            }
            ImGui.Spacing();
            ImGui.Text($"Total Upload:   {FormatMemory(currentUpload)}");
            ImGui.Text($"Total Download: {FormatMemory(currentDownload)}");
            ImGui.Spacing();
            ImGui.Text($"Upload Packets:   {uploadPackets}");
            ImGui.Text($"Download Packets: {downloadPackets}");
        }

        if (ImGui.CollapsingHeader("Graphs", ImGuiTreeNodeFlags.DefaultOpen))
        {
            _uploadGraph.Draw(40f, 1024 * 2);
            ImGui.Spacing();
            _downloadGraph.Draw(40f, 1024 * 512f);
        }
    }

    private static string FormatMemory(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / 1024.0 / 1024.0:F2} MB";
        return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
    }
}
