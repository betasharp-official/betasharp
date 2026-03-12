using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public static class Mouse
{
    public const int EVENT_SIZE = 1 + 1 + 4 + 4 + 4 + 8;

    private static bool s_created;
    private static Glfw s_glfw;
    private static unsafe WindowHandle* s_window;

    private static readonly bool[] s_buttons = new bool[8];
    private static int s_x, s_y;
    private static int s_dx, s_dy, s_dwheel;

    private static readonly Queue<MouseEvent> s_eventQueue = new();

    private static int s_eventButton;
    private static bool s_eventState;
    private static int s_event_dx, s_event_dy, s_event_dwheel;
    private static int s_event_x, s_event_y;
    private static long s_event_nanos;

    private static int s_last_event_raw_x, s_last_event_raw_y;

    private static bool s_isGrabbed;
    private static int s_grab_x, s_grab_y;
    private static bool s_discardNextMove = false;

    private static int s_displayWidth = 800;
    private static int s_displayHeight = 600;

    public static unsafe void create(Glfw glfwApi, WindowHandle* windowHandle, int width, int height)
    {
        if (s_created) return;

        s_glfw = glfwApi;
        s_window = windowHandle;
        s_displayWidth = width;
        s_displayHeight = height;

        // Set up callbacks
        s_glfw.SetCursorPosCallback(s_window, OnCursorPos);
        s_glfw.SetMouseButtonCallback(s_window, OnMouseButton);
        s_glfw.SetScrollCallback(s_window, OnScroll);

        // Get initial position
        s_glfw.GetCursorPos(s_window, out double initX, out double initY);
        s_x = s_last_event_raw_x = (int)initX;
        s_y = s_last_event_raw_y = (int)initY;

        s_created = true;
    }

    private static unsafe void OnCursorPos(WindowHandle* window, double xpos, double ypos)
    {
        if (!s_created) return;

        if (s_discardNextMove)
        {
            s_discardNextMove = false;
            return;
        }

        int newX = (int)xpos;
        int newY = (int)ypos;

        s_dx += newX - s_x;
        s_dy += newY - s_y;

        s_x = newX;
        s_y = newY;

        s_eventQueue.Enqueue(new MouseEvent
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
        if (!s_created) return;

        int buttonIndex = (int)button;
        bool pressed = action == InputAction.Press;

        s_glfw.GetCursorPos(window, out double xpos, out double ypos);

        // Update button state
        if (buttonIndex >= 0 && buttonIndex < s_buttons.Length)
        {
            s_buttons[buttonIndex] = pressed;
        }

        // Queue button event
        s_eventQueue.Enqueue(new MouseEvent
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
        if (!s_created) return;

        s_glfw.GetCursorPos(window, out double xpos, out double ypos);

        // Queue scroll event
        s_eventQueue.Enqueue(new MouseEvent
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
        if (!s_created) throw new InvalidOperationException("Mouse must be created before you can read events");

        if (s_eventQueue.Count > 0)
        {
            MouseEvent evt = s_eventQueue.Dequeue();

            s_eventButton = evt.Button;
            s_eventState = evt.State;
            s_event_nanos = evt.Nanos;

            if (s_isGrabbed)
            {
                // In grabbed mode, report deltas
                s_event_dx = evt.X - s_last_event_raw_x;
                s_event_dy = evt.Y - s_last_event_raw_y;
                s_event_x += s_event_dx;
                s_event_y += s_event_dy;
                s_last_event_raw_x = evt.X;
                s_last_event_raw_y = evt.Y;
            }
            else
            {
                // In non-grabbed mode, report absolute coordinates
                int new_event_x = evt.X;
                int new_event_y = evt.Y;
                s_event_dx = new_event_x - s_last_event_raw_x;
                s_event_dy = new_event_y - s_last_event_raw_y;
                s_event_x = new_event_x;
                s_event_y = new_event_y;
                s_last_event_raw_x = new_event_x;
                s_last_event_raw_y = new_event_y;
            }

            // Clamp to display bounds
            s_event_x = Math.Min(s_displayWidth - 1, Math.Max(0, s_event_x));
            s_event_y = Math.Min(s_displayHeight - 1, Math.Max(0, s_event_y));

            s_event_dwheel = evt.DWheel;

            return true;
        }

        return false;
    }

    public static int getEventButton() => s_eventButton;
    public static bool getEventButtonState() => s_eventState;
    public static int getEventDX() => s_event_dx;
    public static int getEventDY() => s_event_dy;
    public static int getEventX() => s_event_x;
    public static int getEventY() => s_displayHeight - s_event_y;
    public static int getEventDWheel() => s_event_dwheel;
    public static long getEventNanoseconds() => s_event_nanos;

    public static int getX() => s_x;
    public static int getY() => s_displayHeight - s_y;

    public static int getDX()
    {
        int result = s_dx;
        s_dx = 0;
        return result;
    }

    public static int getDY()
    {
        int result = s_dy;
        s_dy = 0;
        return result;
    }

    public static int getDWheel()
    {
        int result = s_dwheel;
        s_dwheel = 0;
        return result;
    }

    public static bool isButtonDown(int button)
    {
        if (!s_created) throw new InvalidOperationException("Mouse must be created before you can poll the button state");
        if (button >= s_buttons.Length || button < 0) return false;
        return s_buttons[button];
    }

    public static unsafe void setGrabbed(bool grab)
    {
        if (!s_created) return;

        bool wasGrabbed = s_isGrabbed;
        s_isGrabbed = grab;

        if (grab && !wasGrabbed)
        {
            s_grab_x = s_x;
            s_grab_y = s_y;
            s_glfw.SetInputMode(s_window, CursorStateAttribute.Cursor, CursorModeValue.CursorDisabled);
        }
        else if (!grab && wasGrabbed)
        {
            s_glfw.SetInputMode(s_window, CursorStateAttribute.Cursor, CursorModeValue.CursorNormal);
            s_glfw.SetCursorPos(s_window, s_grab_x, s_grab_y);
        }

        // Reset state
        s_glfw.GetCursorPos(s_window, out double xpos, out double ypos);
        s_event_x = s_x = (int)xpos;
        s_event_y = s_y = (int)ypos;
        s_last_event_raw_x = (int)xpos;
        s_last_event_raw_y = (int)ypos;
        s_dx = s_dy = s_dwheel = 0;
    }

    public static unsafe void setCursorPosition(int x, int y)
    {
        OnCursorPos(s_window, x, y);
        s_glfw.SetCursorPos(s_window, x, y);
        s_discardNextMove = true;
    }

    public static unsafe void setCursorVisible(bool visible)
    {
        if (!s_created || s_isGrabbed) return;

        s_glfw.SetInputMode(s_window, CursorStateAttribute.Cursor,
            visible ? CursorModeValue.CursorNormal : CursorModeValue.CursorHidden);
    }

    public static bool isCreated() => s_created;

    public static void destroy()
    {
        if (!s_created) return;
        s_created = false;
        s_eventQueue.Clear();
    }

    public static void ClearEvents()
    {
        if (!s_created) return;
        s_eventQueue.Clear();
    }

    public static void setDisplayDimensions(int width, int height)
    {
        s_displayWidth = width;
        s_displayHeight = height;
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
