using BetaSharp.Client.Diagnostics.Windows;
using BetaSharp.Diagnostics;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace BetaSharp.Client.Diagnostics;

internal sealed class DebugWindowManager
{
    private readonly ImGuiController _imGuiController;
    private readonly List<DebugWindow> _windows;
    private readonly FrameGraph _frameTimeGraph;
    private readonly FrameGraph _msptGraph;

    public DebugWindowManager(ImGuiController imGuiController, BetaSharp game)
    {
        _imGuiController = imGuiController;
        _frameTimeGraph = new FrameGraph("Frame Time (ms)", 240);
        _msptGraph = new FrameGraph("MSPT", 240);

        _windows =
        [
            new ProfilerWindow(),
            new RenderInfoWindow(game),
            new ServerInfoWindow(_msptGraph),
            new ClientInfoWindow(game, _frameTimeGraph),
            new NetworkInfoWindow(),
            new LocalPlayerInfoWindow(game),
            new SystemWindow(game),
        ];
    }

    public void Render(float deltaTime)
    {
        _frameTimeGraph.Push(MetricRegistry.Get(ClientMetrics.FrameTimeMs));

        if (!MetricRegistry.IsStale(ServerMetrics.Mspt))
            _msptGraph.Push(MetricRegistry.Get(ServerMetrics.Mspt));
        else
            _msptGraph.Push(0);

        _imGuiController.Update(deltaTime);
        DrawDashboard();
        foreach (DebugWindow window in _windows)
            window.Draw();
        _imGuiController.Render();
    }

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

    public void ToggleAll()
    {
        bool anyVisible = _windows.Exists(w => w.IsVisible);
        bool next = !anyVisible;
        foreach (DebugWindow window in _windows)
            window.IsVisible = next;
    }
}
