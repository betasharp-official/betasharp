using BetaSharp.Client.Options;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public class GuiSlider : GuiButton
{

    public float sliderValue = 1.0F;
    public bool dragging;
    private float _lastPlayedValue = -1.0f;
    private readonly FloatOption _option;
    private readonly BetaSharp _gameInstance;

    public GuiSlider(int id, int x, int y, FloatOption option, string displayString, float value, BetaSharp game) : base(id, x, y, 150, 20, displayString)
    {
        _option = option;
        sliderValue = value;
        _gameInstance = game;
    }

    public GuiSlider Size(int width, int height)
    {
        Width = width;
        Height = height;
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
                sliderValue = (mouseX - (XPosition + 4)) / (float)(Width - 8);
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

                if (Math.Abs(sliderValue - _lastPlayedValue) > 0.05f)
                {
                    _gameInstance.sndManager.PlayUISound("", "console.scroll", _gameInstance.isControllerMode);
                    _lastPlayedValue = sliderValue;
                }
            }

            GLManager.GL.Color4(1.0F, 1.0F, 1.0F, 1.0F);
            DrawTexturedModalRect(XPosition + (int)(sliderValue * (Width - 8)), YPosition, 0, 66, 4, 20);
            DrawTexturedModalRect(XPosition + (int)(sliderValue * (Width - 8)) + 4, YPosition, 196, 66, 4, 20);
        }
    }

    public override bool MousePressed(BetaSharp game, int mouseX, int mouseY)
    {
        if (base.MousePressed(game, mouseX, mouseY))
        {
            sliderValue = (mouseX - (XPosition + 4)) / (float)(Width - 8);
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
