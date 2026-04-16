using System.Numerics;
using BetaSharp.Client.Diagnostics.Windows;
using BetaSharp.Client.Rendering.Presentation;
using BetaSharp.Profiling;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics;

internal sealed class DebugWindowManager
{
    private readonly Func<bool> _inGameHasFocus;
    private readonly List<DebugWindow> _windows;
    private readonly LiveStatsWindow _liveStatsWindow;
    private readonly ConsoleWindow _consoleWindow;
    private bool _dockInitialized;

    /// <summary>True when the "Game Viewport" ImGui window is the focused window (i.e. the user last clicked the game area).</summary>
    public bool GameViewportFocused { get; private set; }

    /// <summary>Content area size of the "Game Viewport" window from the previous frame, used to size the render target.</summary>
    public Vector2 ViewportSize { get; private set; }

    /// <summary>Top-left screen position of the "Game Viewport" content area, in window pixel coordinates.</summary>
    public Vector2 ViewportPos { get; private set; }

    /// <summary>
    /// Rendered frame image for the viewport preview. Set to <see cref="PresentationViewportImage.Empty"/> to show nothing.
    /// </summary>
    public PresentationViewportImage ViewportImage { get; set; }

    public DebugWindowManager(BetaSharp game, Func<bool> inGameHasFocus)
    {
        _inGameHasFocus = inGameHasFocus;

        var ctx = new DebugWindowContext(game);
        _consoleWindow = new ConsoleWindow(ctx);

        var liveStatsSections = new DebugWindow[]
        {
            new NetworkInfoWindow(),
            new ClientInfoWindow(ctx),
            new LocalPlayerInfoWindow(ctx),
            new ServerInfoWindow(),
        };

        _liveStatsWindow = new LiveStatsWindow(liveStatsSections);

        _windows =
        [
            _liveStatsWindow,
            new SystemWindow(ctx),
            new RenderInfoWindow(),
            new AudioDebugWindow(ctx),
            new ProfilerWindow(),
            _consoleWindow,
            new UIInspectorWindow(ctx)
        ];
    }

    public unsafe void Render(float deltaTime)
    {
        ImGuiIO* io = ImGui.GetIO();
        if (_inGameHasFocus())
        {
            io->ConfigFlags |= ImGuiConfigFlags.NoMouse;
        }
        else
        {
            io->ConfigFlags &= ~ImGuiConfigFlags.NoMouse;
        }

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));
        uint dockspaceId = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.PopStyleColor();

        if (!_dockInitialized)
        {
            _dockInitialized = true;
            ImGuiP.DockBuilderRemoveNode(dockspaceId);
            ImGuiP.DockBuilderAddNode(dockspaceId, ImGuiDockNodeFlags.PassthruCentralNode);
            ImGuiP.DockBuilderSetNodeSize(dockspaceId, ImGui.GetMainViewport().Size);

            uint dockMainId = dockspaceId;
            uint dockIdLeft = 0, dockIdRight = 0, dockIdBottom = 0;
            ImGuiP.DockBuilderSplitNode(dockMainId, ImGuiDir.Left, 0.2f, &dockIdLeft, &dockMainId);
            ImGuiP.DockBuilderSplitNode(dockMainId, ImGuiDir.Right, 0.2f, &dockIdRight, &dockMainId);
            ImGuiP.DockBuilderSplitNode(dockMainId, ImGuiDir.Down, 0.28f, &dockIdBottom, &dockMainId);

            foreach (DebugWindow window in _windows)
            {
                if (window.DefaultDock == DebugDock.None)
                {
                    continue;
                }

                uint targetDock = window.DefaultDock switch
                {
                    DebugDock.Left => dockIdLeft,
                    DebugDock.Right => dockIdRight,
                    DebugDock.Bottom => dockIdBottom,
                    _ => dockMainId
                };
                ImGuiP.DockBuilderDockWindow(window.Title, targetDock);
            }

            ImGuiP.DockBuilderDockWindow("Game Viewport", dockMainId);

            ImGuiP.DockBuilderFinish(dockspaceId);
        }

        using (Profiler.Begin("Dashboard"))
        {
            DrawDashboard();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowBgAlpha(0.0f);

        // Note: NoTitleBar is intentionally omitted so the window remains draggable when undocked.
        // NoMouseInputs is intentionally omitted: when in-game, ImGuiConfigFlags.NoMouse is already
        // set globally, so it's redundant; when the debug UI is open, we need mouse events to reach
        // the title bar so the window can be dragged.
        ImGuiWindowFlags gwFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse;
        using (Profiler.Begin("GameViewport"))
        {
            if (ImGui.Begin("Game Viewport", gwFlags))
            {
                Vector2 contentSize = ImGui.GetContentRegionAvail();
                ViewportSize = contentSize;
                ViewportPos = ImGui.GetCursorScreenPos();

                // Derive focus from whether the mouse is physically inside the viewport rect,
                // since NoMouseInputs prevents IsWindowFocused() from ever being true.
                GameViewportFocused = ImGui.IsMouseHoveringRect(ViewportPos, ViewportPos + contentSize, false);
                if (ViewportImage.IsAvailable && contentSize.X > 0 && contentSize.Y > 0)
                {
                    Vector2 uv0 = ViewportImage.FlipY ? new Vector2(0, 1) : new Vector2(0, 0);
                    Vector2 uv1 = ViewportImage.FlipY ? new Vector2(1, 0) : new Vector2(1, 1);

                    unsafe
                    {
                        ImGui.Image(
                            new ImTextureRef(null, new ImTextureID(ViewportImage.ImGuiTextureId)),
                            contentSize,
                            uv0,
                            uv1);
                    }
                }
            }
            else
            {
                GameViewportFocused = false;
            }
            ImGui.End();
        }
        ImGui.PopStyleVar();

        foreach (DebugWindow window in _windows)
        {
            using (Profiler.Begin(window.Title.Replace(" ", "")))
            {
                window.Draw();
            }
        }
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

    internal static unsafe void ApplyStyle()
    {
        ImGuiStyle* style = ImGui.GetStyle();

        style->WindowRounding = 8f;
        style->ChildRounding = 6f;
        style->FrameRounding = 4f;
        style->PopupRounding = 6f;
        style->ScrollbarRounding = 8f;
        style->GrabRounding = 4f;
        style->TabRounding = 4f;

        style->WindowPadding = new Vector2(12f, 10f);
        style->FramePadding = new Vector2(8f, 4f);
        style->ItemSpacing = new Vector2(8f, 6f);
        style->ItemInnerSpacing = new Vector2(6f, 4f);
        style->ScrollbarSize = 10f;
        style->GrabMinSize = 8f;
        style->WindowBorderSize = 1f;
        style->FrameBorderSize = 0f;
        style->TabBorderSize = 0f;

        Span<Vector4> colors = style->Colors;

        colors[(int)ImGuiCol.Text] = new Vector4(0.92f, 0.92f, 0.92f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.52f, 1.00f);

        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);

        colors[(int)ImGuiCol.Border] = new Vector4(0.12f, 0.12f, 0.12f, 1.00f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);

        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.28f, 0.28f, 0.28f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.32f, 0.32f, 0.32f, 1.00f);

        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.19f, 0.19f, 0.19f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);

        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.19f, 0.19f, 0.19f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.46f, 0.46f, 0.46f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.56f, 0.56f, 0.56f, 1.00f);

        Vector4 accent = new(0.25f, 0.58f, 1.00f, 1.00f);
        Vector4 accentHover = new(0.35f, 0.66f, 1.00f, 1.00f);
        Vector4 accentActive = new(0.18f, 0.48f, 0.90f, 1.00f);
        Vector4 accentFaint = new(0.25f, 0.58f, 1.00f, 0.18f);

        colors[(int)ImGuiCol.CheckMark] = accent;
        colors[(int)ImGuiCol.SliderGrab] = accent;
        colors[(int)ImGuiCol.SliderGrabActive] = accentActive;

        colors[(int)ImGuiCol.Button] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.25f, 0.58f, 1.00f, 0.80f);

        colors[(int)ImGuiCol.Header] = accentFaint;
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.25f, 0.58f, 1.00f, 0.28f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.25f, 0.58f, 1.00f, 0.42f);

        colors[(int)ImGuiCol.Separator] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);
        colors[(int)ImGuiCol.SeparatorActive] = accent;
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.30f, 0.30f, 0.30f, 0.50f);
        colors[(int)ImGuiCol.ResizeGripHovered] = accentHover;
        colors[(int)ImGuiCol.ResizeGripActive] = accent;

        colors[(int)ImGuiCol.Tab] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(0.28f, 0.28f, 0.28f, 1.00f);
        colors[(int)ImGuiCol.TabSelected] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        colors[(int)ImGuiCol.TabSelectedOverline] = accent;
        colors[(int)ImGuiCol.TabDimmed] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);

        colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.25f, 0.58f, 1.00f, 0.35f);
        colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);

        colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.00f, 0.75f, 0.00f, 0.90f);
        colors[(int)ImGuiCol.NavCursor] = accent;
        colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.10f, 0.10f, 0.10f, 0.40f);
        colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.05f, 0.05f, 0.05f, 0.50f);

        colors[(int)ImGuiCol.PlotLines] = accent;
        colors[(int)ImGuiCol.PlotLinesHovered] = accentHover;
        colors[(int)ImGuiCol.PlotHistogram] = accent;
        colors[(int)ImGuiCol.PlotHistogramHovered] = accentHover;

        colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
        colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.12f, 0.12f, 0.12f, 1.00f);
        colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
        colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.00f, 1.00f, 1.00f, 0.03f);

        colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.25f, 0.58f, 1.00f, 0.30f);
    }
}
