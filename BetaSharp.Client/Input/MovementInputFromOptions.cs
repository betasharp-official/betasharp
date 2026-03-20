using BetaSharp.Client.Options;
using BetaSharp.Entities;

namespace BetaSharp.Client.Input;

public class MovementInputFromOptions : MovementInput
{

    private readonly bool[] _movementKeyStates = new bool[10];
    private readonly GameOptions _gameSettings;

    public MovementInputFromOptions(GameOptions options)
    {
        _gameSettings = options;
    }

    public override void checkKeyForMovementInput(int keyCode, bool pressed)
    {
        int actionIndex = -1;
        if (keyCode == _gameSettings.KeyBindForward.keyCode)
        {
            actionIndex = 0;
        }

        if (keyCode == _gameSettings.KeyBindBack.keyCode)
        {
            actionIndex = 1;
        }

        if (keyCode == _gameSettings.KeyBindLeft.keyCode)
        {
            actionIndex = 2;
        }

        if (keyCode == _gameSettings.KeyBindRight.keyCode)
        {
            actionIndex = 3;
        }

        if (keyCode == _gameSettings.KeyBindJump.keyCode)
        {
            actionIndex = 4;
        }

        if (keyCode == _gameSettings.KeyBindSneak.keyCode)
        {
            actionIndex = 5;
        }

        if (actionIndex >= 0)
        {
            _movementKeyStates[actionIndex] = pressed;
        }

    }

    public override void resetKeyState()
    {
        for (int i = 0; i < 10; ++i)
        {
            _movementKeyStates[i] = false;
        }
        ControllerManager.SneakToggle = false;
    }

    public override void updatePlayerMoveState(EntityPlayer player)
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

        ControllerManager.HandleMovement(ref moveStrafe, ref moveForward);

        jump = _movementKeyStates[4];
        sneak = _movementKeyStates[5] || ControllerManager.SneakToggle;
        if (sneak)
        {
            moveStrafe = (float)((double)moveStrafe * 0.3D);
            moveForward = (float)((double)moveForward * 0.3D);
        }
    }
}
