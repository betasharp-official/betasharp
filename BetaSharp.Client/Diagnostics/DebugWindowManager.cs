using BetaSharp.Client.Diagnostics.Windows;
using BetaSharp.Diagnostics;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics;

internal sealed class DebugWindowManager
{
    private readonly Func<bool> _inGameHasFocus;
    private readonly List<DebugWindow> _windows;
    private readonly ClientInfoWindow _clientInfoWindow;
    private readonly ServerInfoWindow _serverInfoWindow;
    private bool _dockInitialized;

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

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0));
        uint dockspaceId = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.PopStyleColor();

        if (!_dockInitialized)
        {
            _dockInitialized = true;
            ImGuiP.DockBuilderRemoveNode(dockspaceId);
            ImGuiP.DockBuilderAddNode(dockspaceId, ImGuiDockNodeFlags.PassthruCentralNode);
            ImGuiP.DockBuilderSetNodeSize(dockspaceId, ImGui.GetMainViewport().Size);

            uint dockMainId = dockspaceId;
            uint dockIdLeft = 0, dockIdRight = 0;
            ImGuiP.DockBuilderSplitNode(dockMainId, ImGuiDir.Left, 0.2f, &dockIdLeft, &dockMainId);
            ImGuiP.DockBuilderSplitNode(dockMainId, ImGuiDir.Right, 0.2f, &dockIdRight, &dockMainId);

            ImGuiP.DockBuilderDockWindow("Debug Dashboard", dockIdLeft);
            ImGuiP.DockBuilderDockWindow("Client Info", dockIdLeft);
            ImGuiP.DockBuilderDockWindow("Server Info", dockIdLeft);
            ImGuiP.DockBuilderDockWindow("Local Player", dockIdLeft);

            ImGuiP.DockBuilderDockWindow("Profiler", dockIdRight);
            ImGuiP.DockBuilderDockWindow("Network Info", dockIdRight);
            ImGuiP.DockBuilderDockWindow("Render Info", dockIdRight);
            ImGuiP.DockBuilderDockWindow("System", dockIdRight);

            ImGuiP.DockBuilderDockWindow("Game Viewport", dockMainId);

            ImGuiP.DockBuilderFinish(dockspaceId);
        }

        DrawDashboard();

        ImGui.SetNextWindowBgAlpha(0.0f);
        ImGuiWindowFlags gwFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground;
        if (ImGui.Begin("Game Viewport", gwFlags)) { }
        ImGui.End();

        foreach (DebugWindow window in _windows)
            window.Draw();
    }

    private void DrawDashboard()
    {
        if (ImGui.Begin("Debug Dashboard"))
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
