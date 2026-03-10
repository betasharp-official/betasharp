using BetaSharp.Client.Options;
using BetaSharp.Entities;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public class MovementInputFromOptions : MovementInput
{

    private readonly bool[] _movementKeyStates = new bool[10];
    private readonly GameOptions _gameSettings;
    private bool _isSneakToggled;
    private bool _wasRightThumbDown;

    public MovementInputFromOptions(GameOptions options)
    {
        _gameSettings = options;
    }

    public override void checkKeyForMovementInput(int var1, bool var2)
    {
        int var3 = -1;
        if (var1 == _gameSettings.KeyBindForward.keyCode)
        {
            var3 = 0;
        }

        if (var1 == _gameSettings.KeyBindBack.keyCode)
        {
            var3 = 1;
        }

        if (var1 == _gameSettings.KeyBindLeft.keyCode)
        {
            var3 = 2;
        }

        if (var1 == _gameSettings.KeyBindRight.keyCode)
        {
            var3 = 3;
        }

        if (var1 == _gameSettings.KeyBindJump.keyCode)
        {
            var3 = 4;
        }

        if (var1 == _gameSettings.KeyBindSneak.keyCode)
        {
            var3 = 5;
        }

        if (var3 >= 0)
        {
            _movementKeyStates[var3] = var2;
        }

    }

    public override void resetKeyState()
    {
        for (int var1 = 0; var1 < 10; ++var1)
        {
            _movementKeyStates[var1] = false;
        }
        _isSneakToggled = false;
        _wasRightThumbDown = false;
    }

    public override void updatePlayerMoveState(EntityPlayer var1)
    {
        moveStrafe = 0.0F;
        moveForward = 0.0F;
        if (_movementKeyStates[0])
        {
            ++moveForward;
        }

        if (_movementKeyStates[1])
        {
            --moveForward;
        }

        if (_movementKeyStates[2])
        {
            ++moveStrafe;
        }

        if (_movementKeyStates[3])
        {
            --moveStrafe;
        }

        if (Controller.IsActive() && BetaSharp.Instance.currentScreen == null)
        {
            float lx = Controller.LeftStickX;
            float ly = Controller.LeftStickY;

            moveStrafe -= lx;
            moveForward -= ly;

            bool rightThumbDown = Controller.IsButtonDown(GamepadButton.RightStick);
            if (rightThumbDown && !_wasRightThumbDown)
            {
                _isSneakToggled = !_isSneakToggled;
            }
            _wasRightThumbDown = rightThumbDown;
        }

        jump = _movementKeyStates[4];
        sneak = _movementKeyStates[5] || _isSneakToggled;
        if (sneak)
        {
            moveStrafe = (float)((double)moveStrafe * 0.3D);
            moveForward = (float)((double)moveForward * 0.3D);
        }

    }
}
