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

    protected int _width;
    protected int _height;
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
        _width = 200;
        _height = 20;
        Enabled = true;
        Visible = true;
        Id = _id;
        XPosition = xPos;
        YPosition = yPos;
        _width = wid;
        _height = hei;
        DisplayString = displayStr;
    }

    public GuiButton Size(int width, int height)
    {
        _width = width;
        _height = height;
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

        bool isHovered = mouseX >= XPosition && mouseY >= YPosition && mouseX < XPosition + _width && mouseY < YPosition + _height;
        HoverState hoverState = GetHoverState(isHovered);

        DrawTexturedModalRect(XPosition, YPosition, 0, 46 + (int)hoverState * 20, _width / 2, _height);
        DrawTexturedModalRect(XPosition + _width / 2, YPosition, 200 - _width / 2, 46 + (int)hoverState * 20, _width / 2, _height);

        MouseDragged(game, mouseX, mouseY);

        if (!Enabled)
        {
            DrawCenteredString(font, DisplayString, XPosition + _width / 2, YPosition + (_height - 8) / 2, Color.GrayA0);
        }
        else if (isHovered)
        {
            DrawCenteredString(font, DisplayString, XPosition + _width / 2, YPosition + (_height - 8) / 2, Color.HoverYellow);
        }
        else
        {
            DrawCenteredString(font, DisplayString, XPosition + _width / 2, YPosition + (_height - 8) / 2, Color.GrayE0);
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
        return Enabled && mouseX >= XPosition && mouseY >= YPosition && mouseX < XPosition + _width && mouseY < YPosition + _height;
    }
}
