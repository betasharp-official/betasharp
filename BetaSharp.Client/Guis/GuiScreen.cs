using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;
using java.awt;
using java.awt.datatransfer;
using java.util;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL.Legacy;
using System;
using System.Collections.Generic;

namespace BetaSharp.Client.Guis;

public class GuiScreen : Gui
{
    private static readonly ILogger<GuiScreen> s_logger = Log.Instance.For<GuiScreen>();

    public BetaSharp Game;
    public int Width;
    public int Height;
    protected List<GuiButton> _controlList = new();
    public bool AllowUserInput = false;
    public virtual bool PausesGame => true;
    public TextRenderer FontRenderer;
    public GuiParticle ParticlesGui;
    private GuiButton SelectedButton = null;
    protected bool _isSubscribedToKeyboard = false;

    public virtual void Render(int mouseX, int mouseY, float partialTicks)
    {
        foreach (var control in _controlList)
        {
            control.DrawButton(Game, mouseX, mouseY);
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
            foreach (var control in _controlList.ToArray())
            {
                if (control.MousePressed(Game, mouseX, mouseY))
                {
                    SelectedButton = control;
                    Game.sndManager.PlaySoundFX("random.click", 1.0F, 1.0F);
                    ActionPerformed(control);
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

    protected virtual void ActionPerformed(GuiButton var1) { }

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
    }

    public virtual void HandleMouseInput()
    {
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

    public void HandleKeyboardInput()
    {
        if (Keyboard.getEventKeyState())
        {
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

    public void DrawWorldBackground(int var1)
    {
        if (Game.world != null)
        {
            DrawGradientRect(0, 0, Width, Height, Color.WorldBackgroundDark, Color.WorldBackground);
        }
        else
        {
            DrawBackground(var1);
        }
    }

    public void DrawBackground(int var1)
    {
        GLManager.GL.Disable(EnableCap.Lighting);
        GLManager.GL.Disable(EnableCap.Fog);

        Tessellator tess = Tessellator.instance;
        Game.textureManager.BindTexture(Game.textureManager.GetTextureId("/gui/background.png"));
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

        float scale = 32.0F;
        tess.startDrawingQuads();
        tess.setColorOpaque_I(0x404040);

        tess.addVertexWithUV(0.0D, Height, 0.0D, 0.0D, (double)(Height / scale + var1));
        tess.addVertexWithUV(Width, Height, 0.0D, (double)(Width / scale), (double)(Height / scale + var1));
        tess.addVertexWithUV(Width, 0.0D, 0.0D, (double)(Width / scale), 0 + var1);
        tess.addVertexWithUV(0.0D, 0.0D, 0.0D, 0.0D, 0 + var1);
        tess.draw();
    }

    public virtual void DeleteWorld(bool var1, int var2) { }

    public virtual void SelectNextField() { }
}
