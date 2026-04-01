using BetaSharp.Client.Diagnostics;
using BetaSharp.Diagnostics;
using ImGuiNET;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class ClientInfoWindow(BetaSharp game) : DebugWindow
{
    public override string Title => "Client Info";

    protected override void OnDraw()
    {
        if (ImGui.CollapsingHeader("Performance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"FPS:        {MetricRegistry.Get(ClientMetrics.Fps)}");
            ImGui.Text($"Frame Time: {MetricRegistry.Get(ClientMetrics.FrameTimeMs):F2} ms");
        }

        if (ImGui.CollapsingHeader("Memory", ImGuiTreeNodeFlags.DefaultOpen))
        {
            long maxMem  = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            long usedMem = Environment.WorkingSet;
            long heapMem = GC.GetTotalMemory(false);

            ImGui.Text($"Used: {FormatMb(usedMem)} / {FormatMb(maxMem)} MB");
            ImGui.Text($"Heap: {FormatMb(heapMem)} MB");
        }

        if (ImGui.CollapsingHeader("World", ImGuiTreeNodeFlags.DefaultOpen))
        {
            string chunkInfo = game.World?.GetDebugInfo() ?? "No world loaded.";
            ImGui.Text(chunkInfo);
        }
    }

    private static string FormatMb(long bytes) => bytes > 0 ? $"{bytes / 1024L / 1024L}" : "N/A";
}
