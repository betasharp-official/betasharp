namespace BetaSharp.Client.Diagnostics.Windows;

/// <summary>
/// A specialized debug window that aggregates multiple diagnostic windows into a single docked panel.
/// </summary>
internal sealed class LiveStatsWindow(IEnumerable<DebugWindow> sections) : DebugWindow
{
    public override string Title => "Live Stats";

    public override DebugDock DefaultDock => DebugDock.Left;

    protected override void OnDraw()
    {
        foreach (DebugWindow section in sections)
        {
            section.DrawSection();
        }
    }
}
