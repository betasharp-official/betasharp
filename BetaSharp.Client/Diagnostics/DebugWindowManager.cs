using BetaSharp.Client.Diagnostics.Windows;
using BetaSharp.Client.Input;
using BetaSharp.Diagnostics;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace BetaSharp.Client.Diagnostics;

internal sealed class DebugWindowManager
{
    private static readonly Dictionary<Keys, ImGuiKey> s_keyMap = new()
    {
        { Keys.Tab, ImGuiKey.Tab },
        { Keys.Left, ImGuiKey.LeftArrow },
        { Keys.Right, ImGuiKey.RightArrow },
        { Keys.Up, ImGuiKey.UpArrow },
        { Keys.Down, ImGuiKey.DownArrow },
        { Keys.PageUp, ImGuiKey.PageUp },
        { Keys.PageDown, ImGuiKey.PageDown },
        { Keys.Home, ImGuiKey.Home },
        { Keys.End, ImGuiKey.End },
        { Keys.Insert, ImGuiKey.Insert },
        { Keys.Delete, ImGuiKey.Delete },
        { Keys.Backspace, ImGuiKey.Backspace },
        { Keys.Space, ImGuiKey.Space },
        { Keys.Enter, ImGuiKey.Enter },
        { Keys.KeypadEnter, ImGuiKey.KeypadEnter },
        { Keys.Escape, ImGuiKey.Escape },
        { Keys.ControlLeft, ImGuiKey.LeftCtrl },
        { Keys.ControlRight, ImGuiKey.RightCtrl },
        { Keys.ShiftLeft, ImGuiKey.LeftShift },
        { Keys.ShiftRight, ImGuiKey.RightShift },
        { Keys.AltLeft, ImGuiKey.LeftAlt },
        { Keys.AltRight, ImGuiKey.RightAlt },
        { Keys.SuperLeft, ImGuiKey.LeftSuper },
        { Keys.SuperRight, ImGuiKey.RightSuper },
        { Keys.A, ImGuiKey.A }, { Keys.B, ImGuiKey.B }, { Keys.C, ImGuiKey.C },
        { Keys.D, ImGuiKey.D }, { Keys.E, ImGuiKey.E }, { Keys.F, ImGuiKey.F },
        { Keys.G, ImGuiKey.G }, { Keys.H, ImGuiKey.H }, { Keys.I, ImGuiKey.I },
        { Keys.J, ImGuiKey.J }, { Keys.K, ImGuiKey.K }, { Keys.L, ImGuiKey.L },
        { Keys.M, ImGuiKey.M }, { Keys.N, ImGuiKey.N }, { Keys.O, ImGuiKey.O },
        { Keys.P, ImGuiKey.P }, { Keys.Q, ImGuiKey.Q }, { Keys.R, ImGuiKey.R },
        { Keys.S, ImGuiKey.S }, { Keys.T, ImGuiKey.T }, { Keys.U, ImGuiKey.U },
        { Keys.V, ImGuiKey.V }, { Keys.W, ImGuiKey.W }, { Keys.X, ImGuiKey.X },
        { Keys.Y, ImGuiKey.Y }, { Keys.Z, ImGuiKey.Z },
        { Keys.Number0, ImGuiKey._0 }, { Keys.Number1, ImGuiKey._1 }, { Keys.Number2, ImGuiKey._2 },
        { Keys.Number3, ImGuiKey._3 }, { Keys.Number4, ImGuiKey._4 }, { Keys.Number5, ImGuiKey._5 },
        { Keys.Number6, ImGuiKey._6 }, { Keys.Number7, ImGuiKey._7 }, { Keys.Number8, ImGuiKey._8 },
        { Keys.Number9, ImGuiKey._9 },
    };

    private readonly ImGuiController _imGuiController;
    private readonly Func<bool> _inGameHasFocus;
    private readonly List<DebugWindow> _windows;
    private readonly ClientInfoWindow _clientInfoWindow;
    private readonly ServerInfoWindow _serverInfoWindow;

    public DebugWindowManager(ImGuiController imGuiController, BetaSharp game, Func<bool> inGameHasFocus)
    {
        _imGuiController = imGuiController;
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

        Keyboard.OnCharacterTyped += ForwardCharToImGui;
        Keyboard.OnGlfwKey += ForwardKeyToImGui;
    }

    public void Render(float deltaTime)
    {
        _clientInfoWindow.PushFrameTime(MetricRegistry.Get(ClientMetrics.FrameTimeMs));
        _serverInfoWindow.PushMspt(MetricRegistry.IsStale(ServerMetrics.Mspt) ? 0 : MetricRegistry.Get(ServerMetrics.Mspt));

        // Block ImGui mouse interaction while the player controls the camera.
        // Must be set before Update() because the controller calls NewFrame() internally,
        // at which point ImGui clears the mouse position itself when NoMouse is set.
        ImGuiIOPtr io = ImGui.GetIO();
        if (_inGameHasFocus())
            io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
        else
            io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;

        _imGuiController.Update(deltaTime);

        DrawDashboard();
        foreach (DebugWindow window in _windows)
            window.Draw();
        _imGuiController.Render();
    }

    private void ForwardCharToImGui(char c)
    {
        if (_inGameHasFocus()) return;
        ImGui.GetIO().AddInputCharacter(c);
    }

    private void ForwardKeyToImGui(Keys key, InputAction action, KeyModifiers mods)
    {
        if (_inGameHasFocus()) return;
        if (!s_keyMap.TryGetValue(key, out ImGuiKey imguiKey)) return;

        bool pressed = action == InputAction.Press || action == InputAction.Repeat;
        ImGuiIOPtr io = ImGui.GetIO();
        io.AddKeyEvent(imguiKey, pressed);

        // Keep modifier key state consistent
        io.AddKeyEvent(ImGuiKey.ModCtrl, mods.HasFlag(KeyModifiers.Control));
        io.AddKeyEvent(ImGuiKey.ModShift, mods.HasFlag(KeyModifiers.Shift));
        io.AddKeyEvent(ImGuiKey.ModAlt, mods.HasFlag(KeyModifiers.Alt));
        io.AddKeyEvent(ImGuiKey.ModSuper, mods.HasFlag(KeyModifiers.Super));
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
