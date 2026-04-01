using BetaSharp.Profiling;

namespace BetaSharp.Client.Diagnostics.Windows;

/// <summary>
/// Wraps the existing <see cref="ProfilerRenderer"/> which manages its own ImGui windows,
/// so the standard Begin/End wrapping from <see cref="DebugWindow"/> is skipped.
/// </summary>
internal sealed class ProfilerWindow : DebugWindow
{
    public override string Title => "Profiler";

    public override void Draw()
    {
        if (!IsVisible) return;
        ProfilerRenderer.Draw();
        ProfilerRenderer.DrawGraph();
    }
}
