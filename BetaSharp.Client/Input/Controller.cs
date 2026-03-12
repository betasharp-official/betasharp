using Microsoft.Extensions.Logging;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public static class Controller
{
    private static bool s_created;
    private static Glfw? s_glfw;
    private static unsafe WindowHandle* s_window;
    private static readonly ILogger s_logger = Log.Instance.For("BetaSharp.Client.Input.Controller");

    private static int s_gamepadJoystickIndex = -1;

    private static readonly bool[] s_buttons = new bool[15];
    private static readonly float[] s_axes = new float[6];

    private static readonly bool[] s_lastButtons = new bool[15];

    private static readonly Queue<ControllerEvent> s_eventQueue = new();
    private static ControllerEvent s_current_event = new();

    public static unsafe void Create(Glfw glfwApi, WindowHandle* windowHandle)
    {
        if (s_created) return;
        s_glfw = glfwApi;
        s_window = windowHandle;
        s_created = true;

        for (int i = 0; i < 16; i++)
        {
            if (s_glfw.JoystickPresent(i))
            {
                bool isGamepad = s_glfw.JoystickIsGamepad(i);
                string name = s_glfw.GetJoystickName(i);

                if (isGamepad)
                {
                    if (s_gamepadJoystickIndex == -1 || name.Contains("Xbox", StringComparison.OrdinalIgnoreCase))
                    {
                        s_gamepadJoystickIndex = i;
                        s_logger.LogInformation("Selected gamepad: {}", name);

                        //TODO: IDK IF THIS WORKS
                        if (name.Contains("Xbox", StringComparison.OrdinalIgnoreCase)) 
                            Guis.ControlTooltip.ControllerType = "xone";
                        else if (name.Contains("DualSense", StringComparison.OrdinalIgnoreCase) || name.Contains("PS5", StringComparison.OrdinalIgnoreCase)) 
                            Guis.ControlTooltip.ControllerType = "ps5";
                        else if (name.Contains("DualShock 4", StringComparison.OrdinalIgnoreCase) || name.Contains("PS4", StringComparison.OrdinalIgnoreCase)) 
                            Guis.ControlTooltip.ControllerType = "ps4";
                        else if (name.Contains("DualShock 3", StringComparison.OrdinalIgnoreCase) || name.Contains("PS3", StringComparison.OrdinalIgnoreCase)) 
                            Guis.ControlTooltip.ControllerType = "ps3";
                        else
                            Guis.ControlTooltip.ControllerType = "xone";
                    }
                }
            }
        }
    }

    public static bool IsGamepadConnected()
    {
        if (!s_created || s_gamepadJoystickIndex == -1 || s_glfw == null) return false;
        return s_glfw.JoystickIsGamepad(s_gamepadJoystickIndex);
    }

    public static unsafe void PollEvents()
    {
        if (!s_created || s_glfw == null) return;
        if (!IsGamepadConnected())
        {
            return;
        }

        bool success = s_glfw.GetGamepadState(s_gamepadJoystickIndex, out GamepadState state);
        if (!success)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            s_axes[i] = state.Axes[i];
        }

        for (int i = 0; i < 15; i++)
        {
            s_lastButtons[i] = s_buttons[i];
            bool isDown = state.Buttons[i] == 1;
            s_buttons[i] = isDown;

            if (isDown != s_lastButtons[i])
            {
                s_eventQueue.Enqueue(new ControllerEvent
                {
                    Button = i,
                    State = isDown,
                    Nanos = DateTime.UtcNow.Ticks * 100
                });
            }
        }
    }

    public static bool Next()
    {
        if (!s_created) return false;

        if (s_eventQueue.Count > 0)
        {
            s_current_event = s_eventQueue.Dequeue();
            return true;
        }

        return false;
    }

    public static void ClearEvents()
    {
        if (!s_created) return;
        s_eventQueue.Clear();
    }

    public static int GetEventButton() => s_current_event.Button;
    public static bool GetEventButtonState() => s_current_event.State;

    public static bool IsButtonDown(GamepadButton button)
    {
        int btnIdx = (int)button;
        if (!s_created || btnIdx < 0 || btnIdx >= 15) return false;
        return s_buttons[btnIdx];
    }

    public static float GetAxis(int axisIdx)
    {
        if (!s_created || axisIdx < 0 || axisIdx >= 6) return 0f;

        float axisValue = s_axes[axisIdx];

        if (axisIdx == 0 || axisIdx == 1)
        {
            if (Math.Abs(axisValue) > LeftStickDeadzone)
            {
                return axisValue;
            }
        }
        else if (axisIdx == 2 || axisIdx == 3)
        {
            if (Math.Abs(axisValue) > RightStickDeadzone)
            {
                return axisValue;
            }
        }
        else
        {
            return axisValue;
        }

        return 0.0f;
    }

    public static float LeftStickX => GetAxis(0);
    public static float LeftStickY => GetAxis(1);
    public static float RightStickX => GetAxis(2);
    public static float RightStickY => GetAxis(3);
    public static float LeftTrigger => (GetAxis(4) + 1.0f) / 2.0f;
    public static float RightTrigger => (GetAxis(5) + 1.0f) / 2.0f;

    public static float LeftStickDeadzone => 0.1f;
    public static float RightStickDeadzone => 0.1f;

    public static bool IsActive()
    {
        if (!IsGamepadConnected()) return false;

        float deadzone = 0.2f;
        if (Math.Abs(GetAxis(0)) > deadzone || Math.Abs(GetAxis(1)) > deadzone ||
            Math.Abs(GetAxis(2)) > deadzone || Math.Abs(GetAxis(3)) > deadzone)
        {
            return true;
        }

        for (int i = 0; i < 15; i++)
        {
            if (s_buttons[i]) return true;
        }

        return false;
    }

    private struct ControllerEvent
    {
        public int Button;
        public bool State;
        public long Nanos;
    }
}
