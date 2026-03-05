using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public static class Mouse
{
    public const int EVENT_SIZE = 1 + 1 + 4 + 4 + 4 + 8;

    private static bool created;
    private static Glfw glfw;
    private static unsafe WindowHandle* window;

    // Current state
    private static readonly bool[] buttons = new bool[8];
    private static int x, y;
    private static int absolute_x, absolute_y;
    private static int dx, dy, dwheel;

    // Event queue
    private static readonly Queue<MouseEvent> eventQueue = new();

    // Current event being processed
    private static int eventButton;
    private static bool eventState;
    private static int event_dx, event_dy, event_dwheel;
    private static int event_x, event_y;
    private static long event_nanos;

    // Last positions for delta calculation
    private static int last_event_raw_x, last_event_raw_y;

    // Grab state
    private static bool _isGrabbed;
    private static int grab_x, grab_y;
    private static bool _discardNextMove = false;

    // Display dimensions (set from outside)
    private static int displayWidth = 800;
    private static int displayHeight = 600;

    public static unsafe void create(Glfw glfwApi, WindowHandle* windowHandle, int width, int height)
    {
        if (created) return;

        glfw = glfwApi;
        window = windowHandle;
        displayWidth = width;
        displayHeight = height;

        // Set up callbacks
        glfw.SetCursorPosCallback(window, OnCursorPos);
        glfw.SetMouseButtonCallback(window, OnMouseButton);
        glfw.SetScrollCallback(window, OnScroll);

        // Get initial position
        glfw.GetCursorPos(window, out double initX, out double initY);
        x = absolute_x = last_event_raw_x = (int)initX;
        y = absolute_y = last_event_raw_y = (int)initY;

        created = true;
    }

    private static unsafe void OnCursorPos(WindowHandle* window, double xpos, double ypos)
    {
        if (!created) return;

        if (_discardNextMove) {
            _discardNextMove = false;
            return;
        }

        int newX = (int)xpos;
        int newY = (int)ypos;

        dx += newX - x;
        dy += newY - y;

        x = newX;
        y = newY;
        absolute_x = newX;
        absolute_y = newY;

        eventQueue.Enqueue(new MouseEvent
        {
            Button = -1,
            State = false,
            X = newX,
            Y = newY,
            DWheel = 0,
            Nanos = GetNanos()
        });
    }

    private static unsafe void OnMouseButton(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        if (!created) return;

        int buttonIndex = (int)button;
        bool pressed = action == InputAction.Press;

        glfw.GetCursorPos(window, out double xpos, out double ypos);

        // Update button state
        if (buttonIndex >= 0 && buttonIndex < buttons.Length)
        {
            buttons[buttonIndex] = pressed;
        }

        // Queue button event
        eventQueue.Enqueue(new MouseEvent
        {
            Button = buttonIndex,
            State = pressed,
            X = (int)xpos,
            Y = (int)ypos,
            DWheel = 0,
            Nanos = GetNanos()
        });
    }

    private static unsafe void OnScroll(WindowHandle* window, double offsetX, double offsetY)
    {
        if (!created) return;

        glfw.GetCursorPos(window, out double xpos, out double ypos);

        // Queue scroll event
        eventQueue.Enqueue(new MouseEvent
        {
            Button = -1,
            State = false,
            X = (int)xpos,
            Y = (int)ypos,
            DWheel = (int)(offsetY * 120), // LWJGL uses 120 units per wheel notch
            Nanos = GetNanos()
        });
    }

    public static bool next()
    {
        if (!created) throw new InvalidOperationException("Mouse must be created before you can read events");

        if (eventQueue.Count > 0)
        {
            MouseEvent evt = eventQueue.Dequeue();

            eventButton = evt.Button;
            eventState = evt.State;
            event_nanos = evt.Nanos;

            if (_isGrabbed)
            {
                // In grabbed mode, report deltas
                event_dx = evt.X - last_event_raw_x;
                event_dy = evt.Y - last_event_raw_y;
                event_x += event_dx;
                event_y += event_dy;
                last_event_raw_x = evt.X;
                last_event_raw_y = evt.Y;
            }
            else
            {
                // In non-grabbed mode, report absolute coordinates
                int new_event_x = evt.X;
                int new_event_y = evt.Y;
                event_dx = new_event_x - last_event_raw_x;
                event_dy = new_event_y - last_event_raw_y;
                event_x = new_event_x;
                event_y = new_event_y;
                last_event_raw_x = new_event_x;
                last_event_raw_y = new_event_y;
            }

            // Clamp to display bounds
            event_x = Math.Min(displayWidth - 1, Math.Max(0, event_x));
            event_y = Math.Min(displayHeight - 1, Math.Max(0, event_y));

            event_dwheel = evt.DWheel;

            return true;
        }

        return false;
    }

    public static int getEventButton() => eventButton;
    public static bool getEventButtonState() => eventState;
    public static int getEventDX() => event_dx;
    public static int getEventDY() => event_dy;
    public static int getEventX() => event_x;
    public static int getEventY() => displayHeight - event_y;
    public static int getEventDWheel() => event_dwheel;
    public static long getEventNanoseconds() => event_nanos;

    public static int getX() => x;
    public static int getY() => displayHeight - y;

    public static int getDX()
    {
        int result = dx;
        dx = 0;
        return result;
    }

    public static int getDY()
    {
        int result = dy;
        dy = 0;
        return result;
    }

    public static int getDWheel()
    {
        int result = dwheel;
        dwheel = 0;
        return result;
    }

    public static bool isButtonDown(int button)
    {
        if (!created) throw new InvalidOperationException("Mouse must be created before you can poll the button state");
        if (button >= buttons.Length || button < 0) return false;
        return buttons[button];
    }

    public static bool isGrabbed() => _isGrabbed;

    public static unsafe void setGrabbed(bool grab)
    {
        if (!created) return;

        bool wasGrabbed = _isGrabbed;
        _isGrabbed = grab;

        if (grab && !wasGrabbed)
        {
            grab_x = x;
            grab_y = y;
            glfw.SetInputMode(window, CursorStateAttribute.Cursor, CursorModeValue.CursorDisabled);
        }
        else if (!grab && wasGrabbed)
        {
            glfw.SetInputMode(window, CursorStateAttribute.Cursor, CursorModeValue.CursorNormal);
            glfw.SetCursorPos(window, grab_x, grab_y);
        }

        // Reset state
        glfw.GetCursorPos(window, out double xpos, out double ypos);
        event_x = x = (int)xpos;
        event_y = y = (int)ypos;
        last_event_raw_x = (int)xpos;
        last_event_raw_y = (int)ypos;
        dx = dy = dwheel = 0;
    }

    public static unsafe void setCursorPosition(int x, int y)
    {
        OnCursorPos(window, x, y);
        glfw.SetCursorPos(window, x, y);
        _discardNextMove = true;
    }

    public static bool isCreated() => created;

    public static void destroy()
    {
        if (!created) return;
        created = false;
        eventQueue.Clear();
    }

    public static void setDisplayDimensions(int width, int height)
    {
        displayWidth = width;
        displayHeight = height;
    }

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
    }
}