using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics;

internal enum DebugDock
{
    None,
    Center,
    Left,
    Right,
    Bottom
}

internal abstract class DebugWindow
{
    public abstract string Title { get; }
    public bool IsVisible { get; set; } = true;

    /// <summary>The window's preferred docking position in the debug dockspace.</summary>
    public virtual DebugDock DefaultDock => DebugDock.None;

    public virtual void Draw()
    {
        if (!IsVisible) return;
        bool visible = IsVisible;
        if (ImGui.Begin(Title, ref visible))
            OnDraw();
        // ImGui.End must always be called, even if Begin returned false (collapsed/clipped).
        ImGui.End();
        IsVisible = visible;
    }

    protected virtual void OnDraw() { }

    /// <summary>
    /// Renders this window's content inline (no ImGui Begin/End), wrapped in a collapsing header.
    /// Used when composing multiple windows into a single panel.
    /// </summary>
    internal void DrawSection()
    {
        if (!IsVisible) return;
        if (ImGui.CollapsingHeader(Title, ImGuiTreeNodeFlags.DefaultOpen))
            OnDraw();
    }
}
