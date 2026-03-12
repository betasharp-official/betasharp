using BetaSharp.Client.Rendering;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class GuiButton : Gui
{
    public enum HoverState
    {
        Disabled = 0,
        Normal = 1,
        Hovered = 2
    }

    public int Width;
    public int Height;
    public int XPosition;
    public int YPosition;
    public string DisplayString;
    public int Id;
    public bool Enabled;
    public bool Visible;

    public GuiButton(int id, int xPos, int yPos, string displayStr) : this(id, xPos, yPos, 200, 20, displayStr)
    {

    }

    public GuiButton(int _id, int xPos, int yPos, int wid, int hei, string displayStr)
    {
        Width = 200;
        Height = 20;
        Enabled = true;
        Visible = true;
        Id = _id;
        XPosition = xPos;
        YPosition = yPos;
        Width = wid;
        Height = hei;
        DisplayString = displayStr;
    }

    public GuiButton Size(int width, int height)
    {
        Width = width;
        Height = height;
        return this;
    }

    protected virtual HoverState GetHoverState(bool isMouseOver)
    {
        if (!Enabled) return HoverState.Disabled;
        if (isMouseOver) return HoverState.Hovered;

        return HoverState.Normal;
    }

    public void DrawButton(BetaSharp game, int mouseX, int mouseY)
    {
        if (!Visible) return;

        TextRenderer font = game.fontRenderer;

        game.textureManager.BindTexture(game.textureManager.GetTextureId("/gui/gui.png"));
        GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);

        bool isHovered = mouseX >= XPosition && mouseY >= YPosition && mouseX < XPosition + Width && mouseY < YPosition + Height;
        HoverState hoverState = GetHoverState(isHovered);

        DrawTexturedModalRect(XPosition, YPosition, 0, 46 + (int)hoverState * 20, Width / 2, Height);
        DrawTexturedModalRect(XPosition + Width / 2, YPosition, 200 - Width / 2, 46 + (int)hoverState * 20, Width / 2, Height);

        MouseDragged(game, mouseX, mouseY);

        if (!Enabled)
        {
            DrawCenteredString(font, DisplayString, XPosition + Width / 2, YPosition + (Height - 8) / 2, Color.GrayA0);
        }
        else if (isHovered)
        {
            DrawCenteredString(font, DisplayString, XPosition + Width / 2, YPosition + (Height - 8) / 2, Color.HoverYellow);
        }
        else
        {
            DrawCenteredString(font, DisplayString, XPosition + Width / 2, YPosition + (Height - 8) / 2, Color.GrayE0);
        }
    }

    protected virtual void MouseDragged(BetaSharp game, int mouseX, int mouseY)
    {
    }

    public virtual void MouseReleased(int mouseX, int mouseY)
    {
    }

    public virtual bool MousePressed(BetaSharp game, int mouseX, int mouseY)
    {
        return Enabled && Visible && mouseX >= XPosition && mouseY >= YPosition && mouseX < XPosition + Width && mouseY < YPosition + Height;
    }
}
