using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public class Slider : UIElement
{
    public float Value { get; set; } = 0f;
    public string Text { get; set; } = "";
    public Action<float>? OnValueChanged;

    public Slider()
    {
        Style.Width = 200;
        Style.Height = 20;

        OnMouseDown += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                UpdateValueFromMouse(e.MouseX);
                e.Handled = true;
            }
        };

        OnMouseMove += (e) =>
        {
            UpdateValueFromMouse(e.MouseX);
            e.Handled = true;
        };
    }

    private void UpdateValueFromMouse(int mouseX)
    {
        float relativeX = mouseX - ScreenX;
        Value = Math.Clamp(relativeX / ComputedWidth, 0f, 1f);
        OnValueChanged?.Invoke(Value);
    }

    public override void Render(UIRenderer renderer)
    {
        TextureHandle texture = BetaSharp.Instance.textureManager.GetTextureId("/gui/gui.png");

        renderer.DrawTexturedModalRect(texture, 0, 0, 0, 46, ComputedWidth / 2, ComputedHeight);
        renderer.DrawTexturedModalRect(texture, ComputedWidth / 2, 0, 200 - ComputedWidth / 2, 46, ComputedWidth / 2, ComputedHeight);

        int knobWidth = 8;
        float knobX = Value * (ComputedWidth - knobWidth);

        renderer.DrawTexturedModalRect(texture, knobX, 0, 0, 66, knobWidth / 2f, ComputedHeight);
        renderer.DrawTexturedModalRect(texture, knobX + knobWidth / 2f, 0, 200 - knobWidth / 2f, 66, knobWidth / 2f, ComputedHeight);

        Color tColor = IsHovered ? Color.HoverYellow : Color.GrayE0;
        renderer.DrawCenteredText(Text, ComputedWidth / 2, ComputedHeight / 2 - 4, tColor);

        base.Render(renderer);
    }
}
