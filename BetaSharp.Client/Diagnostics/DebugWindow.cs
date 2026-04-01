using ImGuiNET;

namespace BetaSharp.Client.Diagnostics;

internal abstract class DebugWindow
{
    public abstract string Title { get; }
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Called by <see cref="DebugWindowManager"/> each frame. The default implementation wraps
    /// <see cref="OnDraw"/> in an <c>ImGui.Begin/End</c> pair and handles the close button.
    /// Override entirely for windows that manage their own ImGui windows (e.g. the profiler).
    /// </summary>
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
}
