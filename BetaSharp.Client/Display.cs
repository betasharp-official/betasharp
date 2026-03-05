using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace BetaSharp.Client;

/// <summary>
/// Display manager class that provides functionality similar to LWJGL's Display class.
/// Only one display may be open at once.
/// </summary>
public static unsafe class Display
{
    private static readonly object _lock = new();
    private static IWindow? _window;
    private static GL? _gl;
    private static readonly Glfw? _glfw;

    // Display properties
    private static DisplayMode _currentMode;
    private static readonly DisplayMode _initialMode;
    private static string _title = "Game";
    private static bool _fullscreen;
    private static int _swapInterval;
    private static bool _resizable = true;
    private static bool _wasResized;
    private static bool _closeRequested;
    public static int MSAA_Samples = 0;
    public static bool DebugMode = false;

    // Window position
    private static int _x = -1;
    private static int _y = -1;

    // Background color
    private static float _r, _g, _b;

    static Display()
    {
        // Initialize GLFW
        _glfw = Glfw.GetApi();
        if (!_glfw.Init())
        {
            throw new Exception("Failed to initialize GLFW");
        }

        // Get initial display mode (primary monitor)
        Silk.NET.GLFW.Monitor* monitor = _glfw.GetPrimaryMonitor();
        Silk.NET.GLFW.VideoMode* videoMode = _glfw.GetVideoMode(monitor);
        _initialMode = new DisplayMode(videoMode->Width, videoMode->Height,
            videoMode->RefreshRate, videoMode->RedBits + videoMode->GreenBits + videoMode->BlueBits);
        _currentMode = _initialMode;
    }

    /// <summary>
    /// Return true if the window's native peer has been created.
    /// </summary>
    public static bool isCreated()
    {
        lock (_lock) { return _window != null; }
    }

    /// <summary>
    /// Return the current display mode, as set by setDisplayMode().
    /// </summary>
    public static DisplayMode getDisplayMode()
    {
        lock (_lock) { return _currentMode; }
    }

    /// <summary>
    /// Return the initial desktop display mode.
    /// </summary>
    public static DisplayMode getDesktopDisplayMode()
    {
        return _initialMode;
    }

    /// <summary>
    /// Returns the entire list of possible fullscreen display modes as an array.
    /// Only DisplayModes from this call can be used when the Display is in fullscreen mode.
    /// </summary>
    public static DisplayMode[] getAvailableDisplayModes()
    {
        lock (_lock)
        {
            var modes = new List<DisplayMode>();
            Silk.NET.GLFW.Monitor* monitor = _glfw!.GetPrimaryMonitor();

            unsafe
            {
                int count;
                Silk.NET.GLFW.VideoMode* videoModes = _glfw.GetVideoModes(monitor, out count);

                for (int i = 0; i < count; i++)
                {
                    Silk.NET.GLFW.VideoMode mode = videoModes[i];
                    int bpp = mode.RedBits + mode.GreenBits + mode.BlueBits;
                    modes.Add(new DisplayMode(mode.Width, mode.Height, mode.RefreshRate, bpp));
                }
            }

            // Remove duplicates
            return [.. modes.Distinct()];
        }
    }

    /// <summary>
    /// Set the current display mode. If no OpenGL context has been created, the given mode will apply to
    /// the context when create() is called.
    /// </summary>
    public static void setDisplayMode(DisplayMode mode)
    {
        lock (_lock)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode));

            bool wasFullscreen = isFullscreen();
            _currentMode = mode;

            if (!isCreated())
                return;

            if (_fullscreen)
            {
                switchDisplayMode();
            }
            else
            {
                _window!.Size = new Vector2D<int>(mode.getWidth(), mode.getHeight());
            }
        }
    }

    /// <summary>
    /// Return whether the Display is in fullscreen mode.
    /// </summary>
    public static bool isFullscreen()
    {
        lock (_lock) { return _fullscreen && _currentMode.isFullscreenCapable(); }
    }

    /// <summary>
    /// Set the fullscreen mode of the context.
    /// </summary>
    public static void setFullscreen(bool fullscreen)
    {
        setDisplayModeAndFullscreenInternal(fullscreen, _currentMode);
    }

    /// <summary>
    /// Set the mode of the context.
    /// </summary>
    public static void setDisplayModeAndFullscreen(DisplayMode mode)
    {
        setDisplayModeAndFullscreenInternal(mode.isFullscreenCapable(), mode);
    }

    private static void setDisplayModeAndFullscreenInternal(bool fullscreen, DisplayMode mode)
    {
        lock (_lock)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode));

            bool wasFullscreen = isFullscreen();
            DisplayMode oldMode = _currentMode;

            _currentMode = mode;
            _fullscreen = fullscreen;

            if (!isCreated() || wasFullscreen == isFullscreen() && mode.Equals(oldMode))
                return;

            if (isFullscreen())
            {
                switchDisplayMode();
            }
            else
            {
                resetDisplayMode();
                _window!.Size = new Vector2D<int>(mode.getWidth(), mode.getHeight());
            }
        }
    }

    private static void switchDisplayMode()
    {
        if (!_currentMode.isFullscreenCapable())
        {
            throw new InvalidOperationException("Only modes from getAvailableDisplayModes() can be used for fullscreen");
        }

        if (_window != null && _glfw != null)
        {
            Silk.NET.GLFW.Monitor* monitor = _glfw.GetPrimaryMonitor();
            unsafe
            {
                _glfw.SetWindowMonitor(
                    (WindowHandle*)_window.Handle,
                    monitor,
                    0, 0,
                    _currentMode.getWidth(),
                    _currentMode.getHeight(),
                    _currentMode.getFrequency()
                );
            }
        }
    }

    private static void resetDisplayMode()
    {
        if (_window != null && _glfw != null)
        {
            unsafe
            {
                _glfw.SetWindowMonitor(
                    (WindowHandle*)_window.Handle,
                    null,
                    getWindowX(),
                    getWindowY(),
                    _currentMode.getWidth(),
                    _currentMode.getHeight(),
                    0
                );
            }
        }
    }

    /// <summary>
    /// Return the title of the window.
    /// </summary>
    public static string getTitle()
    {
        lock (_lock) { return _title; }
    }

    /// <summary>
    /// Set the title of the window. This may be ignored by the underlying OS.
    /// </summary>
    public static void setTitle(string title)
    {
        lock (_lock)
        {
            _title = title ?? "";
            if (isCreated())
                _window!.Title = _title;
        }
    }

    /// <summary>
    /// Return true if the user or operating system has asked the window to close.
    /// </summary>
    public static bool isCloseRequested()
    {
        lock (_lock)
        {
            if (!isCreated())
                throw new InvalidOperationException("Cannot determine close requested state of uncreated window");
            return _closeRequested;
        }
    }

    /// <summary>
    /// Return true if the window is visible, false if not.
    /// </summary>
    public static bool isVisible()
    {
        lock (_lock)
        {
            if (!isCreated())
                throw new InvalidOperationException("Cannot determine minimized state of uncreated window");
            return _window!.IsVisible;
        }
    }

    /// <summary>
    /// Return true if window is active, that is, the foreground display of the operating system.
    /// </summary>
    public static bool isActive()
    {
        lock (_lock)
        {
            if (!isCreated())
                throw new InvalidOperationException("Cannot determine focused state of uncreated window");
            return _glfw!.GetWindowAttrib((WindowHandle*)_window!.Handle, WindowAttributeGetter.Focused);
        }
    }

    /// <summary>
    /// Set the window's location. This is a no-op on fullscreen windows.
    /// </summary>
    public static void setLocation(int x, int y)
    {
        lock (_lock)
        {
            _x = x;
            _y = y;

            if (isCreated() && !isFullscreen())
            {
                _window!.Position = new Vector2D<int>(getWindowX(), getWindowY());
            }
        }
    }

    private static int getWindowX()
    {
        if (!isFullscreen())
        {
            if (_x == -1)
                return Math.Max(0, (_initialMode.getWidth() - _currentMode.getWidth()) / 2);
            return _x;
        }
        return 0;
    }

    private static int getWindowY()
    {
        if (!isFullscreen())
        {
            if (_y == -1)
                return Math.Max(0, (_initialMode.getHeight() - _currentMode.getHeight()) / 2);
            return _y;
        }
        return 0;
    }

    /// <summary>
    /// Return the x position (top-left) of the Display window.
    /// </summary>
    public static int getX()
    {
        if (isFullscreen())
            return 0;
        return _window?.Position.X ?? 0;
    }

    /// <summary>
    /// Return the y position (top-left) of the Display window.
    /// </summary>
    public static int getY()
    {
        if (isFullscreen())
            return 0;
        return _window?.Position.Y ?? 0;
    }

    /// <summary>
    /// Return the width of the Display window.
    /// </summary>
    public static int getWidth()
    {
        if (isFullscreen())
            return _currentMode.getWidth();
        return _window?.Size.X ?? _currentMode.getWidth();
    }

    /// <summary>
    /// Return the height of the Display window.
    /// </summary>
    public static int getHeight()
    {
        if (isFullscreen())
            return _currentMode.getHeight();
        return _window?.Size.Y ?? _currentMode.getHeight();
    }

    /// <summary>
    /// Return true if the Display window is resizable.
    /// </summary>
    public static bool isResizable()
    {
        return _resizable;
    }

    /// <summary>
    /// Enable or disable the Display window to be resized.
    /// </summary>
    public static void setResizable(bool resizable)
    {
        _resizable = resizable;
    }

    /// <summary>
    /// Return true if the Display window has been resized.
    /// </summary>
    public static bool wasResized()
    {
        return _wasResized;
    }

    /// <summary>
    /// Set the initial color of the Display.
    /// </summary>
    public static void setInitialBackground(float r, float g, float b)
    {
        _r = r;
        _g = g;
        _b = b;
    }

    /// <summary>
    /// Set the buffer swap interval.
    /// </summary>
    public static void setSwapInterval(int value)
    {
        lock (_lock)
        {
            _swapInterval = value;
            if (isCreated())
                _window!.VSync = value > 0;
        }
    }

    /// <summary>
    /// Enable or disable vertical monitor synchronization.
    /// </summary>
    public static void setVSyncEnabled(bool sync)
    {
        setSwapInterval(sync ? 1 : 0);
    }

    /// <summary>
    /// Create the OpenGL context.
    /// </summary>
    public static void create()
    {
        lock (_lock)
        {
            if (isCreated())
                throw new InvalidOperationException("Only one LWJGL context may be instantiated at any one time.");

            WindowOptions options = WindowOptions.Default;
            options.Size = new Vector2D<int>(_currentMode.getWidth(), _currentMode.getHeight());
            options.Title = _title;
            options.WindowBorder = _resizable ? WindowBorder.Resizable : WindowBorder.Fixed;
            options.VSync = _swapInterval > 0;
            options.IsVisible = true;
            options.Samples = MSAA_Samples;
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, DebugMode ? ContextFlags.Debug : ContextFlags.Default, new APIVersion(4, 1));

            if (_x >= 0 && _y >= 0)
                options.Position = new Vector2D<int>(_x, _y);

            _window = Window.Create(options);

            _window.Load += onLoad;
            _window.Resize += onResize;
            _window.Closing += onClosing;

            if (isFullscreen())
            {
                switchDisplayMode();
            }

            _window.Initialize();
        }
    }

    private static void onLoad()
    {
        _gl = GL.GetApi(_window);
        _gl.ClearColor(_r, _g, _b, 1.0f);
        _gl.Enable(EnableCap.Multisample);
    }

    private static void onResize(Vector2D<int> size)
    {
        _wasResized = true;
    }

    private static void onClosing()
    {
        _closeRequested = true;
    }

    /// <summary>
    /// Process operating system events.
    /// </summary>
    public static void processMessages()
    {
        lock (_lock)
        {
            if (!isCreated())
                throw new InvalidOperationException("Display not created");

            _window!.DoEvents();
        }
    }

    /// <summary>
    /// Swap the display buffers.
    /// </summary>
    public static void swapBuffers()
    {
        lock (_lock)
        {
            if (!isCreated())
                throw new InvalidOperationException("Display not created");

            _window!.SwapBuffers();
        }
    }

    /// <summary>
    /// Update the window. If the window is visible clears the dirty flag and calls swapBuffers().
    /// </summary>
    public static void update()
    {
        update(true);
    }

    /// <summary>
    /// Update the window.
    /// </summary>
    public static void update(bool processMessages)
    {
        lock (_lock)
        {
            if (!isCreated())
                throw new InvalidOperationException("Display not created");

            _wasResized = false;

            if (_window!.IsVisible)
            {
                swapBuffers();
            }

            if (processMessages)
            {
                Display.processMessages();
            }
        }
    }

    /// <summary>
    /// Destroy the Display.
    /// </summary>
    public static void destroy()
    {
        lock (_lock)
        {
            if (!isCreated())
                return;

            _gl?.Dispose();
            _window?.Close();
            _window?.Dispose();

            _window = null;
            _gl = null;
            _closeRequested = false;
            _wasResized = false;

            resetDisplayMode();
        }
    }

    /// <summary>
    /// Gets the OpenGL context.
    /// </summary>
    public static GL? getGL()
    {
        return _gl;
    }

    public static WindowHandle* getWindowHandle()
    {
        if (!isCreated())
        {
            throw new InvalidOperationException("Cannot get WindowHandle of uncreated Display");
        }

        return (WindowHandle*)_window!.Handle;
    }

    public static Glfw getGlfw()
    {
        return _glfw!;
    }

    public static IWindow getWindow()
    {
        return _window!;
    }
}

/// <summary>
/// Represents a display mode with width, height, refresh rate, and bit depth.
/// </summary>
public class DisplayMode : IEquatable<DisplayMode>
{
    private readonly int width;
    private readonly int height;
    private readonly int freq;
    private readonly int bpp;
    private readonly bool fullscreenCapable;

    public DisplayMode(int width, int height, int freq = 60, int bpp = 32, bool fullscreenCapable = true)
    {
        this.width = width;
        this.height = height;
        this.freq = freq;
        this.bpp = bpp;
        this.fullscreenCapable = fullscreenCapable;
    }

    public int getWidth()
    {
        return width;
    }

    public int getHeight()
    {
        return height;
    }

    public int getFrequency()
    {
        return freq;
    }

    public int getBitsPerPixel()
    {
        return bpp;
    }

    public bool isFullscreenCapable()
    {
        return fullscreenCapable;
    }

    public bool Equals(DisplayMode? other)
    {
        if (other == null) return false;
        return width == other.width &&
               height == other.height &&
               freq == other.freq &&
               bpp == other.bpp;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DisplayMode);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(width, height, freq, bpp);
    }

    public override string ToString()
    {
        return $"{width}x{height} @ {freq}Hz ({bpp}bpp)";
    }
}
