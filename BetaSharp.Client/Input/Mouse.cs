using System.Runtime.InteropServices;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public partial class Mouse
{
    public const int EventSize = 1 + 1 + 4 + 4 + 4 + 8;
    public const int MouseButtons = 8;
    private const int DoubleClickThresholdPixels = 4;

    private static Mouse _originalInstance = null!;
    public static Mouse Instance { get; private set; } = null!;

    private static bool created;

    private Glfw _glfw;
    private unsafe WindowHandle* _window;

    // Current state
    private readonly bool[] _buttons = new bool[MouseButtons];
    private int _y;
    private int _absolute_x, _absolute_y;
    private int _dx, _dy, _dwheel;

    // Event queue
    private readonly Queue<MouseEvent> _eventQueue = new();

    // Current event being processed
    private int _event_y;

    // Click tracking for double-click/multi-click detection
    private readonly long[] _lastClickTime = new long[MouseButtons];
    private readonly int[] _lastClickX = new int[MouseButtons];
    private readonly int[] _lastClickY = new int[MouseButtons];
    private readonly int[] _clickCounts = new int[MouseButtons];

    // Last positions for delta calculation
    private int _last_event_raw_x, _last_event_raw_y;

    // Grab state
    private bool _isGrabbedState;
    private int _grab_x, _grab_y;

    // Display dimensions (set from outside)
    private int _displayWidth = 800;
    private int _displayHeight = 600;

    private readonly long _doubleClickDelayNanos = GetSystemDoubleClickTime() * 1_000_000L;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetDoubleClickTime();

    private static int GetSystemDoubleClickTime()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                return (int)GetDoubleClickTime();
            }
            catch
            {
                return 300;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO: Unless someone knows of an easy way to do objc interop in C#, we'll have to use our default.
            return 300;
        }

        return 300;
    }

    /// <summary>
    /// Creates the primary singleton Mouse instance for the game window.
    /// </summary>
    public static unsafe void create(Glfw glfwApi, WindowHandle* windowHandle, int width, int height)
    {
        if (created) return;

        _originalInstance = CreateInstance(glfwApi, windowHandle, width, height);
        ResetInstance();
        created = true;
    }

    public static void ResetInstance() => Instance = _originalInstance;
    public static void SetInstance(Mouse mouse) => Instance = mouse;

    /// <summary>
    /// Creates a new Mouse instance bound to the given window. Can be used for secondary windows
    /// (e.g. dev tools).
    /// </summary>
    public static unsafe Mouse CreateInstance(Glfw glfwApi, WindowHandle* windowHandle, int width, int height)
    {
        Mouse mouse = new()
        {
            _glfw = glfwApi,
            _window = windowHandle,
            _displayWidth = width,
            _displayHeight = height,
        };

        // Set up callbacks
        glfwApi.SetCursorPosCallback(windowHandle, mouse.OnCursorPos);
        glfwApi.SetMouseButtonCallback(windowHandle, mouse.OnMouseButton);
        glfwApi.SetScrollCallback(windowHandle, mouse.OnScroll);

        // Get initial position
        glfwApi.GetCursorPos(windowHandle, out double initX, out double initY);
        mouse.X = mouse._absolute_x = mouse._last_event_raw_x = (int)initX;
        mouse._y = mouse._absolute_y = mouse._last_event_raw_y = (int)initY;

        return mouse;
    }

    private unsafe void OnCursorPos(WindowHandle* window, double xpos, double ypos)
    {
        int newX = (int)xpos;
        int newY = (int)ypos;

        _dx += newX - X;
        _dy += newY - _y;

        X = newX;
        _y = newY;
        _absolute_x = newX;
        _absolute_y = newY;

        _eventQueue.Enqueue(new MouseEvent
        {
            Button = -1,
            State = false,
            X = newX,
            Y = newY,
            DWheel = 0,
            Nanos = GetNanos(),
        });
    }

    private unsafe void OnMouseButton(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        int buttonIndex = (int)button;
        bool pressed = action == InputAction.Press;

        _glfw.GetCursorPos(window, out double xpos, out double ypos);
        int posX = (int)xpos;
        int posY = (int)ypos;
        long nanos = GetNanos();

        // Update button state
        if (buttonIndex >= 0 && buttonIndex < _buttons.Length)
        {
            _buttons[buttonIndex] = pressed;
        }

        // Calculate click count on button press
        int clickCount = 1;
        if (pressed && buttonIndex is >= 0 and < MouseButtons)
        {
            long timeSinceLastClick = nanos - _lastClickTime[buttonIndex];
            int distX = Math.Abs(posX - _lastClickX[buttonIndex]);
            int distY = Math.Abs(posY - _lastClickY[buttonIndex]);

            if (timeSinceLastClick <= _doubleClickDelayNanos &&
                distX <= DoubleClickThresholdPixels &&
                distY <= DoubleClickThresholdPixels)
            {
                clickCount = _clickCounts[buttonIndex] + 1;
            }
            else
            {
                clickCount = 1;
            }

            _lastClickTime[buttonIndex] = nanos;
            _lastClickX[buttonIndex] = posX;
            _lastClickY[buttonIndex] = posY;
            _clickCounts[buttonIndex] = clickCount;
        }

        // Queue button event
        _eventQueue.Enqueue(new MouseEvent
        {
            Button = buttonIndex,
            State = pressed,
            X = posX,
            Y = posY,
            DWheel = 0,
            Nanos = nanos,
            ClickCount = pressed ? clickCount : 0,
        });
    }

    private unsafe void OnScroll(WindowHandle* window, double offsetX, double offsetY)
    {
        _glfw.GetCursorPos(window, out double xpos, out double ypos);

        // Queue scroll event
        _eventQueue.Enqueue(new MouseEvent
        {
            Button = -1,
            State = false,
            X = (int)xpos,
            Y = (int)ypos,
            DWheel = (int)(offsetY * 120), // LWJGL uses 120 units per wheel notch
            Nanos = GetNanos()
        });
    }

    public bool Next()
    {
        if (_eventQueue.Count > 0)
        {
            MouseEvent evt = _eventQueue.Dequeue();

            EventButton = evt.Button;
            EventButtonState = evt.State;
            EventNanoseconds = evt.Nanos;
            EventClickCount = evt.ClickCount;

            if (_isGrabbedState)
            {
                // In grabbed mode, report deltas
                EventDX = evt.X - _last_event_raw_x;
                EventDY = evt.Y - _last_event_raw_y;
                EventX += EventDX;
                _event_y += EventDY;
                _last_event_raw_x = evt.X;
                _last_event_raw_y = evt.Y;
            }
            else
            {
                // In non-grabbed mode, report absolute coordinates
                int new_event_x = evt.X;
                int new_event_y = evt.Y;
                EventDX = new_event_x - _last_event_raw_x;
                EventDY = new_event_y - _last_event_raw_y;
                EventX = new_event_x;
                _event_y = new_event_y;
                _last_event_raw_x = new_event_x;
                _last_event_raw_y = new_event_y;
            }

            // Clamp to display bounds
            EventX = Math.Min(_displayWidth - 1, Math.Max(0, EventX));
            _event_y = Math.Min(_displayHeight - 1, Math.Max(0, _event_y));

            EventDWheel = evt.DWheel;

            return true;
        }

        return false;
    }

    public int EventButton { get; private set; }

    public bool EventButtonState { get; private set; }

    public int EventDX { get; private set; }

    public int EventDY { get; private set; }

    public int EventX { get; private set; }

    public int EventY => _displayHeight - _event_y;

    public int EventDWheel { get; private set; }

    public long EventNanoseconds { get; private set; }

    public int EventClickCount { get; private set; }

    public int X { get; private set; }

    public int Y => _displayHeight - _y;

    public int GetDX()
    {
        int result = _dx;
        _dx = 0;
        return result;
    }

    public int GetDY()
    {
        int result = _dy;
        _dy = 0;
        return result;
    }

    public int GetDWheel()
    {
        int result = _dwheel;
        _dwheel = 0;
        return result;
    }

    public bool IsButtonDown(int button)
    {
        if (button >= _buttons.Length || button < 0) return false;
        return _buttons[button];
    }

    public bool IsGrabbed() => _isGrabbedState;

    public unsafe void SetGrabbed(bool grab)
    {
        bool wasGrabbed = _isGrabbedState;
        _isGrabbedState = grab;

        if (grab && !wasGrabbed)
        {
            _grab_x = X;
            _grab_y = _y;
            _glfw.SetInputMode(_window, CursorStateAttribute.Cursor, CursorModeValue.CursorDisabled);
        }
        else if (!grab && wasGrabbed)
        {
            _glfw.SetInputMode(_window, CursorStateAttribute.Cursor, CursorModeValue.CursorNormal);
            _glfw.SetCursorPos(_window, _grab_x, _grab_y);
        }

        // Reset state
        _glfw.GetCursorPos(_window, out double xpos, out double ypos);
        EventX = X = (int)xpos;
        _event_y = _y = (int)ypos;
        _last_event_raw_x = (int)xpos;
        _last_event_raw_y = (int)ypos;
        _dx = _dy = _dwheel = 0;
    }

    public unsafe void SetCursorPosition(int x, int y)
    {
        _glfw.SetCursorPos(_window, x, y);
    }

    public void Destroy()
    {
        _eventQueue.Clear();
    }

    public void SetDisplayDimensions(int width, int height)
    {
        _displayWidth = width;
        _displayHeight = height;
    }

    public static bool next()
    {
        if (!created) throw new InvalidOperationException("Mouse must be created before you can read events");
        return Instance.Next();
    }

    public static int getEventButton() => Instance.EventButton;
    public static bool getEventButtonState() => Instance.EventButtonState;
    public static int getEventDX() => Instance.EventDX;
    public static int getEventDY() => Instance.EventDY;
    public static int getEventX() => Instance.EventX;
    public static int getEventY() => Instance.EventY;
    public static int getEventDWheel() => Instance.EventDWheel;
    public static long getEventNanoseconds() => Instance.EventNanoseconds;
    public static int getEventClickCount() => Instance.EventClickCount;

    public static int getX() => Instance.X;
    public static int getY() => Instance.Y;

    public static int getDX() => Instance.GetDX();
    public static int getDY() => Instance.GetDY();
    public static int getDWheel() => Instance.GetDWheel();

    public static bool isButtonDown(int button)
    {
        if (!created) throw new InvalidOperationException("Mouse must be created before you can poll the button state");
        return Instance.IsButtonDown(button);
    }

    public static bool isGrabbed() => Instance.IsGrabbed();

    public static void setGrabbed(bool grab)
    {
        if (!created) return;
        Instance.SetGrabbed(grab);
    }

    public static void setCursorPosition(int x, int y) => Instance.SetCursorPosition(x, y);

    public static bool isCreated() => created;

    public static void destroy()
    {
        if (!created) return;
        Instance.Destroy();
        created = false;
    }

    public static void setDisplayDimensions(int width, int height) => Instance.SetDisplayDimensions(width, height);

    private static long GetNanos()
    {
        return DateTime.UtcNow.Ticks * 100; // Convert to nanoseconds
    }

    private struct MouseEvent
    {
        public int Button;
        public bool State;
        public int X;
        public int Y;
        public int DWheel;
        public long Nanos;
        public int ClickCount;
    }
}
