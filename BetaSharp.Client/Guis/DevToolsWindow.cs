using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using Silk.NET.GLFW;
using Silk.NET.OpenGL.Legacy;

namespace BetaSharp.Client.Guis;

public static unsafe class DevToolsWindow
{
    private static readonly Glfw _glfw = Glfw.GetApi();
    private static WindowHandle* _window;
    private static WindowHandle* _gameWindow;
    private static IGL _gl = null!;
    private static DevToolsScreen? _screen;
    private static int _windowWidth = 800;
    private static int _windowHeight = 600;
    private static int _windowX = 100;
    private static int _windowY = 100;
    private static Mouse _mouse = null!;
    private static Keyboard _keyboard = null!;
    private static bool _justOpened;
    private static bool _focused;

    public static bool IsOpen { get; private set; }

    private static void Show()
    {
        if (IsOpen) return;

        _gameWindow = Display.getWindowHandle();
        _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGL);
        _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 4);
        _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 1);

        _window = _glfw.CreateWindow(_windowWidth, _windowHeight, "GUI Inspector", null, _gameWindow);
        _glfw.SetWindowPos(_window, _windowX, _windowY);

        if (_window == null)
            throw new Exception("Failed to create devtools window");

        _glfw.SetWindowSizeCallback(_window, OnWindowResize);
        _glfw.SetWindowPosCallback(_window, (_, x, y) => (_windowX, _windowY) = (x, y));
        _glfw.SetWindowFocusCallback(_window, (_, focused) => _focused = focused);

        _glfw.MakeContextCurrent(_window);
        _gl = new EmulatedGL(GL.GetApi(_glfw.GetProcAddress));

        _screen = new() { EffectiveSize = new(_windowWidth, _windowHeight) };

        IsOpen = true;

        _mouse = Mouse.CreateInstance(_glfw, _window, _windowWidth, _windowHeight);
        _keyboard = Keyboard.CreateInstance(_glfw, _window);

        _glfw.MakeContextCurrent(_gameWindow);
        _justOpened = true;
    }

    private static void OnWindowResize(WindowHandle* window, int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        _windowWidth = width;
        _windowHeight = height;
        _mouse.SetDisplayDimensions(_windowWidth, _windowHeight);
        _screen?.SetWorldAndResolution(Minecraft.INSTANCE, width, height);
    }

    private static void Close()
    {
        if (!IsOpen) return;

        _glfw.DestroyWindow(_window);
        _window = null;
        _screen = null;

        IsOpen = false;

        _glfw.FocusWindow(_gameWindow); // Makes it easier to press F12 twice to close and reopen the window quickly
    }

    public static void Toggle()
    {
        if (IsOpen)
        {
            if (_focused) Close();
            else _glfw.FocusWindow(_window);
        }
        else Show();
    }

    // TODO: don't refresh the entire hierarchy when a control is added, just add/remove the control
    public static void ControlAdded(Control parent, Control child)
    {
        if (IsOpen && parent.ParentScreen != _screen)
            _screen?.RefreshHierarchy(Minecraft.INSTANCE.currentScreen);
    }
    public static void ControlRemoved(Control parent, Control child)
    {
        if (IsOpen && parent.ParentScreen != _screen)
            _screen?.RefreshHierarchy(Minecraft.INSTANCE.currentScreen);
    }

    public static void UpdateAndRender()
    {
        if (!IsOpen || _screen == null) return;

        if (_glfw.WindowShouldClose(_window))
        {
            Close();
            return;
        }

        _glfw.PollEvents();

        _glfw.MakeContextCurrent(_window);
        GLManager.SetGL(_gl);
        Mouse.SetInstance(_mouse);
        Keyboard.SetInstance(_keyboard);

        // The screen always renders at GUI scale 1 for simplicity's sake, and so that as much information is
        // visible at once as possible. We have to set the GUI scale to 1 here or else DoRender will do the GL
        // scissoring and translation based on the wrong scale. Likewise with the display width and height.
        var mc = Minecraft.INSTANCE;

        int oldWidth = mc.displayWidth;
        int oldHeight = mc.displayHeight;
        int oldGuiScale = mc.options.GuiScaleOption.Value;

        mc.options.GuiScaleOption.Value = 1;
        mc.displayWidth = _windowWidth;
        mc.displayHeight = _windowHeight;

        _gl.Viewport(0, 0, (uint)_windowWidth, (uint)_windowHeight);

        _gl.MatrixMode(GLEnum.Projection);
        _gl.LoadIdentity();
        _gl.Ortho(0, _windowWidth, _windowHeight, 0, -1, 1);
        _gl.MatrixMode(GLEnum.Modelview);
        _gl.LoadIdentity();

        _gl.Disable(EnableCap.Lighting);
        _gl.Disable(EnableCap.Fog);
        _gl.Disable(GLEnum.DepthTest);
        _gl.Enable(EnableCap.Texture2D);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        _gl.Enable(GLEnum.AlphaTest);
        _gl.AlphaFunc(GLEnum.Greater, 0.003f); // a little under 1/256
        _gl.Color4(1.0f, 1.0f, 1.0f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);


        if (_justOpened)
        {
            _screen.RefreshHierarchy(Minecraft.INSTANCE.currentScreen);
            _justOpened = false;
        }
        _screen.UpdatePropertyPanel();

        ProcessInput();

        if (IsOpen) // ProcessInput closes the window if F12 is pressed
        {
            _screen.DoRender(new(_mouse.X, _windowHeight - _mouse.Y, 0));
            _glfw.SwapBuffers(_window);
        }

        mc.options.GuiScaleOption.Value = oldGuiScale;
        mc.displayWidth = oldWidth;
        mc.displayHeight = oldHeight;
        GLManager.ResetGL();
        Mouse.ResetInstance();
        Keyboard.ResetInstance();
        _glfw.MakeContextCurrent(_gameWindow);
    }

    private static void ProcessInput()
    {
        _screen?.HandleInput();
    }
}
