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

    public override void checkKeyForMovementInput(int scanCode, bool isPressed)
    {
        int movementIndex = -1;
        if (scanCode == _gameSettings.KeyBindForward.scanCode)
        {
            movementIndex = 0;
        }

        if (scanCode == _gameSettings.KeyBindBack.scanCode)
        {
            movementIndex = 1;
        }

        if (scanCode == _gameSettings.KeyBindLeft.scanCode)
        {
            movementIndex = 2;
        }

        if (scanCode == _gameSettings.KeyBindRight.scanCode)
        {
            movementIndex = 3;
        }

        if (scanCode == _gameSettings.KeyBindJump.scanCode)
        {
            movementIndex = 4;
        }

        if (scanCode == _gameSettings.KeyBindSneak.scanCode)
        {
            movementIndex = 5;
        }

        if (movementIndex >= 0)
        {
            _movementKeyStates[movementIndex] = isPressed;
        }

    }

    public override void resetKeyState()
    {
        for (int keyIndex = 0; keyIndex < 10; ++keyIndex)
        {
            _movementKeyStates[keyIndex] = false;
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
