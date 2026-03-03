using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class GuiSlider : GuiButton
{

    public float sliderValue = 1.0F;
    public bool dragging;
    private readonly FloatOption _option;

    public GuiSlider(int id, int x, int y, FloatOption option, string displayString, float value) : base(id, x, y, 150, 20, displayString)
    {
        _option = option;
        sliderValue = value;
    }

    public GuiSlider Size(int width, int height)
    {
        _width = width;
        _height = height;
        return this;
    }

    protected override HoverState GetHoverState(bool var1)
    {
        return HoverState.Disabled;
    }

    protected override void MouseDragged(BetaSharp game, int mouseX, int mouseY)
    {
        if (Enabled)
        {
            if (dragging)
            {
                sliderValue = (mouseX - (XPosition + 4)) / (float)(_width - 8);
                if (sliderValue < 0.0F)
                {
                    sliderValue = 0.0F;
                }

                if (sliderValue > 1.0F)
                {
                    sliderValue = 1.0F;
                }

                _option.Set(sliderValue);
                DisplayString = _option.GetDisplayString(TranslationStorage.Instance);
            }

            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            DrawTexturedModalRect(XPosition + (int)(sliderValue * (_width - 8)), YPosition, 0, 66, 4, 20);
            DrawTexturedModalRect(XPosition + (int)(sliderValue * (_width - 8)) + 4, YPosition, 196, 66, 4, 20);
        }
    }

    public override bool MousePressed(BetaSharp game, int mouseX, int mouseY)
    {
        if (base.MousePressed(game, mouseX, mouseY))
        {
            sliderValue = (mouseX - (XPosition + 4)) / (float)(_width - 8);
            if (sliderValue < 0.0F)
            {
                sliderValue = 0.0F;
            }

            if (sliderValue > 1.0F)
            {
                sliderValue = 1.0F;
            }

            _option.Set(sliderValue);
            DisplayString = _option.GetDisplayString(TranslationStorage.Instance);
            dragging = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void MouseReleased(int x, int y)
    {
        dragging = false;
    }
}
