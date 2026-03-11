using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using Silk.NET.GLFW;

namespace BetaSharp.Client.Input;

public static class ControllerManager
{
    private static BetaSharp? s_game;

    private static bool s_wasAttackDown;
    private static bool s_wasInteractDown;
    private static bool s_wasInventoryDown;
    private static bool s_wasDropDown;
    private static bool s_wasHotbarLeftDown;
    private static bool s_wasHotbarRightDown;
    private static bool s_wasCameraDown;
    private static bool s_wasPauseDown;
    private static bool s_wasPlayerListDown;
    private static bool s_wasPickBlockDown;
    private static bool s_wasCraftingDown;
    private static bool s_wasSneakDown;
    private static bool s_wasJumpDown;

    public static bool SneakToggle { get; set; }
    private static bool s_suppressInGameInput;

    public static void Initialize(BetaSharp game)
    {
        s_game = game;
    }

    private static void SyncWasStates()
    {
        s_wasAttackDown = Controller.RightTrigger > 0.5f;
        s_wasInteractDown = Controller.LeftTrigger > 0.5f;
        s_wasInventoryDown = Controller.IsButtonDown(GamepadButton.Y);
        s_wasDropDown = Controller.IsButtonDown(GamepadButton.B);
        s_wasHotbarLeftDown = Controller.IsButtonDown(GamepadButton.LeftBumper);
        s_wasHotbarRightDown = Controller.IsButtonDown(GamepadButton.RightBumper);
        s_wasCameraDown = Controller.IsButtonDown(GamepadButton.LeftStick);
        s_wasPauseDown = Controller.IsButtonDown(GamepadButton.Start);
        s_wasPlayerListDown = Controller.IsButtonDown(GamepadButton.Back);
        s_wasPickBlockDown = Controller.IsButtonDown(GamepadButton.DPadUp);
        s_wasSneakDown = Controller.IsButtonDown(GamepadButton.RightStick);
        s_wasCraftingDown = Controller.IsButtonDown(GamepadButton.X);
        s_wasJumpDown = Controller.IsButtonDown(GamepadButton.A);
    }

    public static void UpdateInGame(float tickDelta)
    {
        if (s_game == null || s_game.currentScreen != null || !s_game.inGameHasFocus)
        {
            if (s_game != null) s_suppressInGameInput = true;
            SyncWasStates();
            return;
        }

        bool jumpHeld = Controller.IsButtonDown(GamepadButton.A);
        bool attackHeld = Controller.RightTrigger > 0.5f;
        bool interactHeld = Controller.LeftTrigger > 0.5f;
        bool inventoryHeld = Controller.IsButtonDown(GamepadButton.Y);
        bool dropHeld = Controller.IsButtonDown(GamepadButton.B);
        bool lbHeld = Controller.IsButtonDown(GamepadButton.LeftBumper);
        bool rbHeld = Controller.IsButtonDown(GamepadButton.RightBumper);
        bool cameraHeld = Controller.IsButtonDown(GamepadButton.LeftStick);
        bool pauseHeld = Controller.IsButtonDown(GamepadButton.Start);
        bool playerListHeld = Controller.IsButtonDown(GamepadButton.Back);
        bool pickBlockHeld = Controller.IsButtonDown(GamepadButton.DPadUp);
        bool sneakHeld = Controller.IsButtonDown(GamepadButton.RightStick);
        bool craftingHeld = Controller.IsButtonDown(GamepadButton.X);

        if (s_suppressInGameInput)
        {
            if (!jumpHeld && !attackHeld && !interactHeld && !inventoryHeld && !dropHeld &&
                !lbHeld && !rbHeld && !cameraHeld && !pauseHeld && !playerListHeld &&
                !pickBlockHeld && !sneakHeld && !craftingHeld)
            {
                s_suppressInGameInput = false;
            }
            SyncWasStates();
            return;
        }

        // Jump
        if (jumpHeld != s_wasJumpDown)
        {
            s_game.player.movementInput.checkKeyForMovementInput(s_game.options.KeyBindJump.keyCode, jumpHeld);
        }

        // Attack
        if (attackHeld && !s_wasAttackDown)
        {
            s_game.ClickMouse(0);
            s_game.MouseTicksRan = s_game.TicksRan;
        }
        else if (attackHeld && s_game.TicksRan - s_game.MouseTicksRan >= s_game.Timer.ticksPerSecond / 4.0F)
        {
            s_game.ClickMouse(0);
            s_game.MouseTicksRan = s_game.TicksRan;
        }

        // Interact
        if (interactHeld && !s_wasInteractDown)
        {
            s_game.ClickMouse(1);
            s_game.MouseTicksRan = s_game.TicksRan;
        }
        else if (interactHeld && s_game.TicksRan - s_game.MouseTicksRan >= s_game.Timer.ticksPerSecond / 4.0F)
        {
            s_game.ClickMouse(1);
            s_game.MouseTicksRan = s_game.TicksRan;
        }

        // Inventory
        if (inventoryHeld && !s_wasInventoryDown)
        {
            s_game.displayGuiScreen(new GuiInventory(s_game.player));
        }

        // Crafting (X Button)
        if (craftingHeld && !s_wasCraftingDown)
        {
            s_game.displayGuiScreen(new GuiInventory(s_game.player));
        }

        // Drop
        if (dropHeld && !s_wasDropDown)
        {
            s_game.player.dropSelectedItem();
        }

        // Hotbar
        if (lbHeld && !s_wasHotbarLeftDown) s_game.player.inventory.changeCurrentItem(1);
        if (rbHeld && !s_wasHotbarRightDown) s_game.player.inventory.changeCurrentItem(-1);

        // Camera
        if (cameraHeld && !s_wasCameraDown)
        {
            s_game.options.CameraMode = (EnumCameraMode)((int)(s_game.options.CameraMode + 2) % 3);
        }

        // Sneak Toggle
        if (sneakHeld && !s_wasSneakDown)
        {
            SneakToggle = !SneakToggle;
        }

        // Pause
        if (pauseHeld && !s_wasPauseDown)
        {
            s_game.displayInGameMenu();
        }

        // Pick Block
        if (pickBlockHeld && !s_wasPickBlockDown)
        {
            s_game.ClickMiddleMouseButton();
        }

        // Debug Toggle
        if (playerListHeld && !s_wasPlayerListDown)
        {
            s_game.options.ShowDebugInfo = !s_game.options.ShowDebugInfo;
        }

        if (jumpHeld || attackHeld || interactHeld || inventoryHeld || dropHeld || lbHeld || rbHeld || cameraHeld || pauseHeld || playerListHeld || pickBlockHeld || sneakHeld || craftingHeld)
        {
            s_game.isControllerMode = true;
        }

        SyncWasStates();

        // Drain the controller event queue. UpdateInGame uses polling (IsButtonDown),
        // so events are never consumed. Without this drain, leftover events leak into
        // GUI processing when a screen is opened (e.g. Y opens inventory, then the
        // Y event fires HandleQuickMove in the newly opened GUI).
        while (Controller.Next()) { }
    }

    public static void UpdateGui(GuiScreen screen)
    {
        if (s_game == null || screen == null) return;
        s_suppressInGameInput = true;

        while (Controller.Next())
        {
            s_game.isControllerMode = true;
            screen.HandleControllerInput();
        }
    }

    public static void HandleMovement(ref float moveStrafe, ref float moveForward)
    {
        if (Controller.IsActive() && s_game != null && s_game.currentScreen == null && !s_suppressInGameInput)
        {
            float lx = Controller.LeftStickX;
            float ly = Controller.LeftStickY;

            moveStrafe -= lx;
            moveForward -= ly;
        }
    }

    public static void HandleLook(ref float yawDelta, ref float pitchDelta, float sensitivity)
    {
        if (Controller.IsActive() && !s_suppressInGameInput)
        {
            float rx = Controller.RightStickX;
            float ry = Controller.RightStickY;
            float deadzone = Controller.RightStickDeadzone;

            if (Math.Abs(rx) > deadzone || Math.Abs(ry) > deadzone)
            {
                float activeRx = (Math.Abs(rx) - deadzone) / (1.0f - deadzone);
                yawDelta += activeRx * activeRx * Math.Sign(rx) * 10f * sensitivity;

                float activeRy = (Math.Abs(ry) - deadzone) / (1.0f - deadzone);
                pitchDelta += activeRy * activeRy * Math.Sign(ry) * 10f * sensitivity;
            }
        }
    }
}
