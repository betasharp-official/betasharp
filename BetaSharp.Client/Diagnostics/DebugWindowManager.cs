using BetaSharp.Client.Diagnostics.Windows;
using BetaSharp.Client.Input;
using BetaSharp.Diagnostics;
using Hexa.NET.ImGui;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Diagnostics;

internal sealed class DebugWindowManager
{
    private readonly Func<bool> _inGameHasFocus;
    private readonly List<DebugWindow> _windows;
    private readonly ClientInfoWindow _clientInfoWindow;
    private readonly ServerInfoWindow _serverInfoWindow;

    public DebugWindowManager(BetaSharp game, Func<bool> inGameHasFocus)
    {
        _inGameHasFocus = inGameHasFocus;

        var ctx = new DebugWindowContext(game);
        _clientInfoWindow = new ClientInfoWindow(ctx);
        _serverInfoWindow = new ServerInfoWindow(ctx);

        _windows =
        [
            new ProfilerWindow(),
            new RenderInfoWindow(ctx),
            _serverInfoWindow,
            _clientInfoWindow,
            new NetworkInfoWindow(),
            new LocalPlayerInfoWindow(ctx),
            new SystemWindow(ctx),
        ];
    }

    public unsafe void Render(float deltaTime)
    {
        _clientInfoWindow.PushFrameTime(MetricRegistry.Get(ClientMetrics.FrameTimeMs));
        _serverInfoWindow.PushMspt(MetricRegistry.IsStale(ServerMetrics.Mspt) ? 0 : MetricRegistry.Get(ServerMetrics.Mspt));

        ImGuiIO* io = ImGui.GetIO();
        if (_inGameHasFocus())
            io->ConfigFlags |= ImGuiConfigFlags.NoMouse;
        else
            io->ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

        DrawDashboard();
        foreach (DebugWindow window in _windows)
            window.Draw();
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
