using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Client.Rendering.Core.OpenGL;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.Guis;

public class GuiScreen : Gui
{
    private static readonly ILogger<GuiScreen> s_logger = Log.Instance.For<GuiScreen>();

    public BetaSharp Game;
    public int Width;
    public int Height;
    protected List<GuiButton> _controlList = [];
    public bool AllowUserInput = false;
    public virtual bool PausesGame => true;
    public TextRenderer FontRenderer;
    public GuiParticle ParticlesGui;
    private GuiButton SelectedButton = null;
    protected GuiButton? _hoveredButton = null;
    protected bool _isSubscribedToKeyboard = false;

    public virtual void Render(int mouseX, int mouseY, float partialTicks)
    {
        _hoveredButton = null;
        foreach (GuiButton control in _controlList)
        {
            control.DrawButton(Game, mouseX, mouseY);
            if (mouseX >= control.XPosition && mouseY >= control.YPosition && mouseX < control.XPosition + control.Width && mouseY < control.YPosition + control.Height)
            {
                _hoveredButton = control;
            }
        }

        ControlTooltip.Render(Game, Width, Height, partialTicks);
    }

    public virtual void GetTooltips(List<ActionTip> tips)
    {
        if (_hoveredButton != null)
        {
            tips.Add(new(ControlIcon.A, "Select"));
        }
    }

    protected virtual void KeyTyped(char eventChar, int eventKey)
    {
        if (eventKey == Keyboard.KEY_ESCAPE)
        {
            Game.displayGuiScreen(null);
            Game.setIngameFocus();
        }
    }

    protected virtual void CharTyped(char eventChar) { }

    public static string GetClipboardString()
    {
        unsafe
        {
            if (Display.isCreated())
            {
                return Display.getGlfw().GetClipboardString(Display.getWindowHandle());
            }
        }

        return "";
    }

    public static void SetClipboardString(string text)
    {
        try
        {
            unsafe
            {
                if (Display.isCreated())
                {
                    Display.getGlfw().SetClipboardString(Display.getWindowHandle(), text);
                }
            }
        }
        catch (Exception)
        {
            s_logger.LogError($"Failed to set clipboard string: {text}");
        }
    }

    protected virtual void MouseClicked(int mouseX, int mouseY, int button)
    {
        if (button == 0)
        {
            foreach (GuiButton control in _controlList.ToArray())
            {
                if (control.MousePressed(Game, mouseX, mouseY))
                {
                    SelectedButton = control;
                    Game.sndManager.PlaySoundFX(Game.isControllerMode ? "random.wood click" : "random.click", 1.0F, 1.0F);
                    ActionPerformed(control);
                    break;
                }
            }
        }
    }

    protected virtual void MouseMovedOrUp(int x, int y, int button)
    {
        if (SelectedButton != null && button == 0)
        {
            SelectedButton.MouseReleased(x, y);
            SelectedButton = null;
        }
    }

    protected virtual void ActionPerformed(GuiButton button) { }

    public void SetWorldAndResolution(BetaSharp game, int width, int height)
    {
        ParticlesGui = new GuiParticle(game);
        this.Game = game;
        FontRenderer = game.fontRenderer;
        Width = width;
        Height = height;
        _controlList.Clear();
        InitGui();
    }

    public virtual void InitGui()
    {
    }

    public void HandleInput()
    {
        while (Mouse.next())
        {
            HandleMouseInput();
        }

        while (Keyboard.Next())
        {
            HandleKeyboardInput();
        }

        ControllerManager.UpdateGui(this);
    }

    public virtual void HandleMouseInput()
    {
        if (Mouse.getEventDX() != 0 || Mouse.getEventDY() != 0 || Mouse.getEventButton() != -1)
        {
            Game.isControllerMode = false;
            Mouse.setCursorVisible(true);
        }

        int x = Mouse.getEventX() * Width / Game.displayWidth;
        int y = Height - Mouse.getEventY() * Height / Game.displayHeight - 1;
        if (Mouse.getEventButtonState())
        {
            MouseClicked(x, y, Mouse.getEventButton());
        }
        else
        {
            MouseMovedOrUp(x, y, Mouse.getEventButton());
        }
    }

    public virtual void HandleControllerInput()
    {
        int scaledMouseX = (int)(Game.virtualCursorX * Width / Game.displayWidth);
        int scaledMouseY = (int)(Game.virtualCursorY * Height / Game.displayHeight);

        if (Controller.GetEventButton() == (int)Silk.NET.GLFW.GamepadButton.A)
        {
            if (Controller.GetEventButtonState())
            {
                MouseClicked(scaledMouseX, scaledMouseY, 0);
            }
            else
            {
                MouseMovedOrUp(scaledMouseX, scaledMouseY, 0);
            }
        }
        else if (Controller.GetEventButton() == (int)Silk.NET.GLFW.GamepadButton.X)
        {
            if (Controller.GetEventButtonState())
            {
                MouseClicked(scaledMouseX, scaledMouseY, 1);
            }
            else
            {
                MouseMovedOrUp(scaledMouseX, scaledMouseY, 1);
            }
        }
        else if (Controller.GetEventButton() == (int)Silk.NET.GLFW.GamepadButton.B)
        {
            if (Controller.GetEventButtonState())
            {
                KeyTyped('\0', Keyboard.KEY_ESCAPE);
            }
        }
        else if (Controller.GetEventButton() == (int)Silk.NET.GLFW.GamepadButton.Y)
        {
            if (Controller.GetEventButtonState())
            {
                HandleQuickMove(scaledMouseX, scaledMouseY);
            }
        }
        else if (Controller.GetEventButton() == (int)Silk.NET.GLFW.GamepadButton.LeftBumper)
        {
            if (Controller.GetEventButtonState())
            {
                HandleTabLeft();
            }
        }
        else if (Controller.GetEventButton() == (int)Silk.NET.GLFW.GamepadButton.RightBumper)
        {
            if (Controller.GetEventButtonState())
            {
                HandleTabRight();
            }
        }
    }

    protected virtual void HandleQuickMove(int x, int y) { }
    protected virtual void HandleTabLeft() { }
    protected virtual void HandleTabRight() { }

    public void HandleKeyboardInput()
    {
        if (Keyboard.getEventKeyState())
        {
            Game.isControllerMode = false;
            Mouse.setCursorVisible(true);

            int key = Keyboard.getEventKey();
            char c = Keyboard.getEventCharacter();

            if (key == Keyboard.KEY_F11)
            {
                Game.toggleFullscreen();
                return;
            }

            if (key != Keyboard.KEY_NONE)
            {
                KeyTyped(c, key);
            }
        }
    }

    public virtual void UpdateScreen() { }

    public virtual void OnGuiClosed()
    {
        if (_isSubscribedToKeyboard)
        {
            Keyboard.OnCharacterTyped -= CharTyped;
            _isSubscribedToKeyboard = false;
        }
    }

    public void DrawDefaultBackground()
    {
        DrawWorldBackground(0);
    }

    public void DrawWorldBackground(int yOffset)
    {
        if (Game.world != null)
        {
            DrawGradientRect(0, 0, Width, Height, Color.WorldBackgroundDark, Color.WorldBackground);
        }
        else
        {
            DrawBackground(yOffset);
        }
    }

    public void DrawBackground(int yOffset)
    {
        GLManager.GL.Disable(GLEnum.Lighting);
        GLManager.GL.Disable(GLEnum.Fog);

        Tessellator tess = Tessellator.instance;
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/background.png"));
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

        float scale = 32.0F;
        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x404040);

        tess.addVertexWithUV(0.0D, Height, 0.0D, 0.0D, (double)(Height / scale + yOffset));
        tess.addVertexWithUV(Width, Height, 0.0D, (double)(Width / scale), (double)(Height / scale + yOffset));
        tess.addVertexWithUV(Width, 0.0D, 0.0D, (double)(Width / scale), 0 + yOffset);
        tess.addVertexWithUV(0.0D, 0.0D, 0.0D, 0.0D, 0 + yOffset);
        tess.draw();
    }

    public virtual void DeleteWorld(bool confirmed, int worldId) { }

    public virtual void SelectNextField() { }

    public virtual bool HandleDPadNavigation(int dpadX, int dpadY, ref float cursorX, ref float cursorY)
    {
        if (_controlList.Count == 0) return false;

        ScaledResolution sr = new(Game.options, Game.displayWidth, Game.displayHeight);

        int scaledMouseX = (int)(cursorX * sr.ScaledWidth / Game.displayWidth);
        int scaledMouseY = (int)(cursorY * sr.ScaledHeight / Game.displayHeight);

        GuiButton? currentButton = null;
        foreach (GuiButton control in _controlList)
        {
            if (scaledMouseX >= control.XPosition && scaledMouseY >= control.YPosition &&
                scaledMouseX < control.XPosition + control.Width && scaledMouseY < control.YPosition + control.Height)
            {
                currentButton = control;
                break;
            }
        }

        if (currentButton is GuiSlider) return false;

        float refX, refY;
        if (currentButton != null)
        {
            refX = currentButton.XPosition + currentButton.Width / 2;
            refY = currentButton.YPosition + currentButton.Height / 2;
        }
        else
        {
            refX = scaledMouseX;
            refY = scaledMouseY;
        }

        GuiButton? bestButton = null;
        float bestScore = float.MaxValue;

        foreach (GuiButton button in _controlList)
        {
            if (button == currentButton || !button.Visible || !button.Enabled) continue;

            float buttonCenterX = button.XPosition + button.Width / 2;
            float buttonCenterY = button.YPosition + button.Height / 2;

            float dx = buttonCenterX - refX;
            float dy = buttonCenterY - refY;

            if (dpadX > 0 && dx <= 0) continue;
            if (dpadX < 0 && dx >= 0) continue;
            if (dpadY > 0 && dy <= 0) continue;
            if (dpadY < 0 && dy >= 0) continue;

            float primaryDist = dpadX != 0 ? Math.Abs(dx) : Math.Abs(dy);
            float crossDist = dpadX != 0 ? Math.Abs(dy) : Math.Abs(dx);
            float score = primaryDist + crossDist * 3f;

            if (score < bestScore)
            {
                bestScore = score;
                bestButton = button;
            }
        }

        if (bestButton != null)
        {
            float targetScaledX = bestButton.XPosition + bestButton.Width / 2;
            float targetScaledY = bestButton.YPosition + bestButton.Height / 2;
            cursorX = targetScaledX * Game.displayWidth / sr.ScaledWidth;
            cursorY = targetScaledY * Game.displayHeight / sr.ScaledHeight;
            return true;
        }

        return false;
    }
}
