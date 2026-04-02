using BetaSharp.Profiling;

namespace BetaSharp.Client.Diagnostics.Windows;

internal sealed class ProfilerWindow : DebugWindow
{
    public override string Title => "Profiler";

    public override void Draw()
    {
        if (!IsVisible) return;
        ProfilerRenderer.Draw();
    }
}
