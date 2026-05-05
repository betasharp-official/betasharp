using BetaSharp.Client.Options;
using BetaSharp.Client.UI;
using BetaSharp.Client.UI.Screens.InGame.Containers;
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
    private static bool s_wasToggleDebugDown;
    private static bool s_wasPickBlockDown;
    private static bool s_wasCraftingDown;
    private static bool s_wasSneakDown;
    private static bool s_wasJumpDown;
    private static long s_nextZoomInAdjustAtMs;
    private static long s_nextZoomOutAdjustAtMs;

    public static bool SneakToggle { get; set; }
    private static bool s_suppressInGameInput;

    public static void Initialize(BetaSharp game)
    {
        s_game = game;
    }

    private static bool IsActionDown(string actionKey)
    {
        if (s_game == null) return false;
        foreach (ControllerBinding cb in s_game.Options.ControllerBindings)
        {
            if (cb.ActionKey == actionKey)
            {
                if ((int)cb.Button < 0) return false;
                return Controller.IsButtonDown(cb.Button);
            }
        }
        return false;
    }

    public static bool IsZoomHeld()
    {
        if (s_game == null) return false;
        if (s_game.CurrentScreen != null || !s_game.InGameHasFocus) return false;
        return IsActionDown("controller.zoom");
    }

    private static void SyncWasStates()
    {
        s_wasAttackDown = Controller.RightTrigger > 0.5f;
        s_wasInteractDown = Controller.LeftTrigger > 0.5f;
        s_wasInventoryDown = IsActionDown("controller.inventory");
        s_wasDropDown = IsActionDown("controller.drop");
        s_wasHotbarLeftDown = IsActionDown("controller.hotbarLeft");
        s_wasHotbarRightDown = IsActionDown("controller.hotbarRight");
        s_wasCameraDown = IsActionDown("controller.camera");
        s_wasPauseDown = IsActionDown("controller.pause");
        s_wasToggleDebugDown = Controller.IsButtonDown(GamepadButton.Back);
        s_wasPickBlockDown = IsActionDown("controller.pickBlock");
        s_wasSneakDown = IsActionDown("controller.sneak");
        s_wasCraftingDown = IsActionDown("controller.crafting");
        s_wasJumpDown = IsActionDown("controller.jump");
    }

    /// <summary>
    /// Handles controller bindings that should fire regardless of whether the player is
    /// in-game, on a menu, or on the main screen (e.g. the debug overlay toggle).
    /// </summary>
    public static void UpdateGlobal()
    {
        if (s_game == null) return;

        bool toggleDebug = Controller.IsButtonDown(GamepadButton.Back);
        if (toggleDebug && !s_wasToggleDebugDown)
        {
            s_game.Options.ShowDebugInfo = !s_game.Options.ShowDebugInfo;
        }
        s_wasToggleDebugDown = toggleDebug;
    }

    public static void UpdateInGame(float tickDelta)
    {
        if (s_game == null || s_game.CurrentScreen != null || !s_game.InGameHasFocus)
        {
            if (s_game != null) s_suppressInGameInput = true;
            SyncWasStates();
            return;
        }

        bool jumpHeld = IsActionDown("controller.jump");
        bool attackHeld = Controller.RightTrigger > 0.5f;
        bool interactHeld = Controller.LeftTrigger > 0.5f;
        bool inventoryHeld = IsActionDown("controller.inventory");
        bool dropHeld = IsActionDown("controller.drop");
        bool lbHeld = IsActionDown("controller.hotbarLeft");
        bool rbHeld = IsActionDown("controller.hotbarRight");
        bool cameraHeld = IsActionDown("controller.camera");
        bool pauseHeld = IsActionDown("controller.pause");
        bool playerListHeld = Controller.IsButtonDown(GamepadButton.Back);
        bool pickBlockHeld = IsActionDown("controller.pickBlock");
        bool sneakHeld = IsActionDown("controller.sneak");
        bool craftingHeld = IsActionDown("controller.crafting");
        bool zoomHeld = IsActionDown("controller.zoom");

        if (s_suppressInGameInput)
        {
            if (!jumpHeld && !attackHeld && !interactHeld && !inventoryHeld && !dropHeld &&
                !lbHeld && !rbHeld && !cameraHeld && !pauseHeld && !playerListHeld &&
                !pickBlockHeld && !sneakHeld && !craftingHeld && !zoomHeld)
            {
                s_suppressInGameInput = false;
            }
            SyncWasStates();
            return;
        }

        // Jump
        if (jumpHeld != s_wasJumpDown)
        {
            s_game.Player.movementInput.checkKeyForMovementInput(s_game.Options.KeyBindJump.scanCode, jumpHeld);
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
            s_game.Navigate(new InventoryScreen(s_game.UIContext, s_game.Player, s_game.PlayerController, () => s_game.CurrentScreen));
        }

        // Crafting
        if (craftingHeld && !s_wasCraftingDown)
        {
            s_game.Navigate(new InventoryScreen(s_game.UIContext, s_game.Player, s_game.PlayerController, () => s_game.CurrentScreen));
        }

        // Drop
        if (dropHeld && !s_wasDropDown)
        {
            s_game.Player.DropSelectedItem();
        }

        // Hotbar / Zoom adjust
        if (zoomHeld)
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (lbHeld && (!s_wasHotbarLeftDown || nowMs >= s_nextZoomInAdjustAtMs))
            {
                s_game.Options.ZoomScale = Math.Clamp(s_game.Options.ZoomScale * 1.08F, 1.25F, 20.0F);
                s_nextZoomInAdjustAtMs = nowMs + (s_wasHotbarLeftDown ? 70L : 220L);
            }
            else if (!lbHeld)
            {
                s_nextZoomInAdjustAtMs = 0L;
            }

            if (rbHeld && (!s_wasHotbarRightDown || nowMs >= s_nextZoomOutAdjustAtMs))
            {
                s_game.Options.ZoomScale = Math.Clamp(s_game.Options.ZoomScale / 1.08F, 1.25F, 20.0F);
                s_nextZoomOutAdjustAtMs = nowMs + (s_wasHotbarRightDown ? 70L : 220L);
            }
            else if (!rbHeld)
            {
                s_nextZoomOutAdjustAtMs = 0L;
            }
        }
        else
        {
            if (lbHeld && !s_wasHotbarLeftDown) s_game.Player.Inventory.ChangeCurrentItem(1);
            if (rbHeld && !s_wasHotbarRightDown) s_game.Player.Inventory.ChangeCurrentItem(-1);
            s_nextZoomInAdjustAtMs = 0L;
            s_nextZoomOutAdjustAtMs = 0L;
        }

        // Camera
        if (cameraHeld && !s_wasCameraDown)
        {
            s_game.Options.CameraMode = (CameraMode)((int)(s_game.Options.CameraMode + 2) % 3);
        }

        // Sneak Toggle
        if (sneakHeld && !s_wasSneakDown)
        {
            SneakToggle = !SneakToggle;
        }

        // Pause
        if (pauseHeld && !s_wasPauseDown)
        {
            s_game.DisplayInGameMenu();
        }

        // Pick Block
        if (pickBlockHeld && !s_wasPickBlockDown)
        {
            s_game.ClickMiddleMouseButton();
        }

        if (jumpHeld || attackHeld || interactHeld || inventoryHeld || dropHeld || lbHeld || rbHeld ||
            cameraHeld || pauseHeld || playerListHeld || pickBlockHeld || sneakHeld || craftingHeld || zoomHeld)
        {
            s_game.IsControllerMode = true;
        }

        SyncWasStates();

        while (Controller.Next()) { }
    }

    public static void UpdateUI(UIScreen? screen)
    {
        if (s_game == null || screen == null) return;
        s_suppressInGameInput = true;

        while (Controller.Next())
        {
            s_game.IsControllerMode = true;
            screen.HandleControllerInput();
        }
    }

    public static void HandleMovement(ref float moveStrafe, ref float moveForward)
    {
        if (Controller.IsActive() && s_game != null && s_game.CurrentScreen == null && !s_suppressInGameInput)
        {
            float lx = Controller.LeftStickX;
            float ly = Controller.LeftStickY;

            moveStrafe -= lx;
            moveForward -= ly;
        }
    }

    public static void HandleLook(ref float yawDelta, ref float pitchDelta, float mouseSensitivity, float deltaTime)
    {
        if (s_game == null)
        {
            return;
        }

        if (Controller.IsActive() && !s_suppressInGameInput)
        {
            float rx = Controller.RightStickX;
            float ry = Controller.RightStickY;
            float deadzone = Controller.RightStickDeadzone;

            if (Math.Abs(rx) > deadzone || Math.Abs(ry) > deadzone)
            {
                const float mult = 120.0f;

                float sensitivity = s_game.Options.ControllerSensitivity * 0.6f + 0.2f;
                sensitivity = sensitivity * sensitivity * sensitivity * 8.0f;

                float activeRx = (Math.Abs(rx) - deadzone) / (1.0f - deadzone);
                yawDelta += activeRx * activeRx * Math.Sign(rx) * 10f * sensitivity * deltaTime * mult;

                float activeRy = (Math.Abs(ry) - deadzone) / (1.0f - deadzone);
                pitchDelta += activeRy * activeRy * Math.Sign(ry) * 10f * sensitivity * deltaTime * mult;
            }
        }
    }
}
