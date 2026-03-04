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

public class Screen : Control
{
    private static readonly ILogger<Screen> s_logger = Log.Instance.For<Screen>();
    public override bool TopLevel => true;
    public Minecraft MC;
    public bool AllowUserInput = false;
    public virtual bool PausesGame => true;
    public TextRenderer FontRenderer;
    public GuiParticle ParticlesGui;
    protected bool IsSubscribedToKeyboard = false;
    protected bool DisplayTitle;
    private Control? _focusedControl;

    public Screen()
    {
        var mc = Minecraft.INSTANCE;
        ScaledResolution scaledResolution = new(mc.options, mc.displayWidth, mc.displayHeight);
        int scaledWidth = scaledResolution.ScaledWidth;
        int scaledHeight = scaledResolution.ScaledHeight;
        SetWorldAndResolution(mc, scaledWidth, scaledHeight);
        Rendered += (_, _) =>
        {
            if (DisplayTitle)
            {
                Gui.DrawCenteredString(FontRenderer, Text, Width / 2, 20, 0xFFFFFF);
            }
        };
    }

    public static string GetClipboardString()
    {
        unsafe
        {
            if (Display.isCreated())
            {
                return Display.getGlfw().GetClipboardString(Display.getWindowHandle()) ?? "";
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

    public void SetWorldAndResolution(Minecraft mc, int width, int height)
    {
        ParticlesGui = new(mc);
        MC = mc;
        FontRenderer = mc.fontRenderer;
        Size = new(width, height);
    }

    public virtual void OnGuiClosed() { }

    public void DrawDefaultBackground()
    {
        DrawWorldBackground(0);
    }

    public void DrawWorldBackground(int var1)
    {
        if (MC.world != null)
        {
            Gui.DrawGradientRect(0, 0, Width, Height, 0xC0101010, 0xD0101010);
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
        MC.textureManager.BindTexture(MC.textureManager.GetTextureId("/gui/background.png"));
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

    public void SetFocus(Control? control)
    {
        if (_focusedControl == control) return;

        var oldFocused = _focusedControl;
        if (oldFocused != null)
        {
            oldFocused.Focused = false;
            oldFocused.DoFocusChanged(new(false, control));
        }

        _focusedControl = control;
        if (control != null)
        {
            control.Focused = true;
            control.DoFocusChanged(new(true, oldFocused));
        }
    }

    public virtual void DeleteWorld(bool confirmed, int index) { }

    public virtual void SelectNextField()
    {

    }

    protected override void OnMousePress(MouseEventArgs e)
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
            if (Keyboard.getEventKeyState() && Keyboard.getEventKey() == Keyboard.KEY_ESCAPE)
            {
                MC.OpenScreen(null);
                return;
            }
            if (_focusedControl != null)
            {
                _focusedControl.HandleKeyboardInput();
            }
            else
            {
                HandleKeyboardInput();
            }
        }
    }
}
