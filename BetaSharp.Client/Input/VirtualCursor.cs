using BetaSharp.Client.Options;
using BetaSharp.Client.UI;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public sealed class VirtualCursor
{
    private float _x;
    private float _y;

    private bool _wasDpadLeftDown;
    private bool _wasDpadRightDown;
    private bool _wasDpadUpDown;
    private bool _wasDpadDownDown;

    public float X => _x;
    public float Y => _y;

    public void Reset(int displayWidth, int displayHeight)
    {
        _x = displayWidth / 2.0f;
        _y = displayHeight / 2.0f;
    }

    public void Update(UIScreen? currentScreen, GameOptions options, int displayWidth, int displayHeight, float deltaTime)
    {
        float lx = Controller.LeftStickX;
        float ly = Controller.LeftStickY;

        bool dpadLeft = Controller.IsButtonDown(GamepadButton.DPadLeft);
        bool dpadRight = Controller.IsButtonDown(GamepadButton.DPadRight);
        bool dpadUp = Controller.IsButtonDown(GamepadButton.DPadUp);
        bool dpadDown = Controller.IsButtonDown(GamepadButton.DPadDown);

        if (currentScreen != null)
        {
            int dpadX = 0, dpadY = 0;
            if (dpadLeft && !_wasDpadLeftDown) dpadX = -1;
            if (dpadRight && !_wasDpadRightDown) dpadX = 1;
            if (dpadUp && !_wasDpadUpDown) dpadY = -1;
            if (dpadDown && !_wasDpadDownDown) dpadY = 1;

            if (dpadX != 0 || dpadY != 0)
                currentScreen.HandleDPadNavigation(dpadX, dpadY, ref _x, ref _y);
        }

        _wasDpadLeftDown = dpadLeft;
        _wasDpadRightDown = dpadRight;
        _wasDpadUpDown = dpadUp;
        _wasDpadDownDown = dpadDown;

        if (currentScreen?.IsEditingSlider == true) return;

        ScaledResolution sr = new(options, displayWidth, displayHeight);
        float speed = 200f * sr.ScaleFactor;

        _x = Math.Clamp(_x + lx * speed * deltaTime, 0, displayWidth);
        _y = Math.Clamp(_y + ly * speed * deltaTime, 0, displayHeight);
    }
}
