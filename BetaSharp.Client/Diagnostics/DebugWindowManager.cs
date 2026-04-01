using BetaSharp.Client.Diagnostics.Windows;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace BetaSharp.Client.Diagnostics;

internal sealed class DebugWindowManager
{
    private readonly ImGuiController _imGuiController;
    private readonly List<DebugWindow> _windows;

    public DebugWindowManager(ImGuiController imGuiController, BetaSharp game)
    {
        _imGuiController = imGuiController;
        _windows =
        [
            new ProfilerWindow(),
            new RenderInfoWindow(game),
            new ServerInfoWindow(),
            new ClientInfoWindow(game),
            new LocalPlayerInfoWindow(game),
            new SystemWindow(game),
        ];
    }

    public void Render(float deltaTime)
    {
        _imGuiController.Update(deltaTime);
        DrawDashboard();
        foreach (DebugWindow window in _windows)
            window.Draw();
        _imGuiController.Render();
    }

    /// <summary>
    /// Small dashboard window with a checkbox per debug window to toggle visibility.
    /// </summary>
    private void DrawDashboard()
    {
        if (ImGui.Begin("Debug"))
        {
            foreach (DebugWindow window in _windows)
            {
                bool visible = window.IsVisible;
                ImGui.Checkbox(window.Title, ref visible);
                window.IsVisible = visible;
            }
        }
        ImGui.End();
    }

    /// <summary>
    /// Toggles all windows off if any are visible, or all on if none are.
    /// Useful for a single hotkey to hide/show the entire debug suite.
    /// </summary>
    public void ToggleAll()
    {
        bool anyVisible = _windows.Exists(w => w.IsVisible);
        bool next = !anyVisible;
        foreach (DebugWindow window in _windows)
            window.IsVisible = next;
    }
}
