using BetaSharp.Diagnostics;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class ServerInfoWindow : DebugWindow
{
    private readonly FrameGraph _msptGraph = new("MSPT", 240);

    public override string Title => "Server Info";

    protected override void OnDraw()
    {
        // Metrics go stale when connected to a remote server because nothing pushes them.
        bool stale = MetricRegistry.IsStale(ServerMetrics.Tps);

        if (stale)
        {
            ImGui.TextDisabled("Remote server — internal data unavailable.");
            ImGui.Separator();
            ImGui.TextDisabled("TPS:      N/A");
            ImGui.TextDisabled("MSPT:     N/A");
            ImGui.TextDisabled("Entities: N/A");
            ImGui.TextDisabled("Players:  N/A");
        }
        else
        {
            float mspt = MetricRegistry.Get(ServerMetrics.Mspt);
            _msptGraph.Push(mspt);

            ImGui.Text($"TPS:      {MetricRegistry.Get(ServerMetrics.Tps):F1}");
            ImGui.Text($"MSPT:     {mspt:F2} ms");
            ImGui.Text($"Entities: {MetricRegistry.Get(ServerMetrics.EntityCount)}");
            ImGui.Text($"Players:  {MetricRegistry.Get(ServerMetrics.PlayerCount)}");

            ImGui.Spacing();
            _msptGraph.Draw(40f, 50.0f);
        }
    }
}
