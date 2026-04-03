using BetaSharp.Diagnostics;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class ClientInfoWindow(DebugWindowContext ctx) : DebugWindow
{
    private readonly FrameGraph _frameTimeGraph = new("Frame Time (ms)", 240);

    public override string Title => "Client Info";

    protected override void OnDraw()
    {
        if (ImGui.CollapsingHeader("Performance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            float frameTimeMs = MetricRegistry.Get(ClientMetrics.FrameTimeMs);
            _frameTimeGraph.Push(frameTimeMs);

            ImGui.Text($"FPS:        {MetricRegistry.Get(ClientMetrics.Fps)}");
            ImGui.Text($"Frame Time: {frameTimeMs:F2} ms");
            ImGui.Spacing();
            _frameTimeGraph.Draw(40f, 0.33f);
        }

        if (ImGui.CollapsingHeader("Memory", ImGuiTreeNodeFlags.DefaultOpen))
        {
            long maxMem = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            long usedMem = Environment.WorkingSet;
            long heapMem = GC.GetTotalMemory(false);

            ImGui.Text($"Used: {FormatMb(usedMem)} / {FormatMb(maxMem)} MB");
            ImGui.Text($"Heap: {FormatMb(heapMem)} MB");
        }

        if (ImGui.CollapsingHeader("World", ImGuiTreeNodeFlags.DefaultOpen))
        {
            string chunkInfo = ctx.World?.GetDebugInfo() ?? "No world loaded.";
            ImGui.Text(chunkInfo);
        }
    }

    private static string FormatMb(long bytes) => bytes > 0 ? $"{bytes / 1024L / 1024L}" : "N/A";
}
