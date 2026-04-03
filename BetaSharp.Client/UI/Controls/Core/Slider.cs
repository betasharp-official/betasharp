using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class Slider : UIElement
{
    public float Value { get; set; } = 0f;
    public string Text { get; set; } = "";
    /// <summary>
    /// The normalized step size for one discrete unit (e.g. 0.01 for a 0–100 range).
    /// Used by controller DPad and stick navigation to move by exactly one value at a time.
    /// </summary>
    public float Step { get; set; } = 0.01f;
    public Action<float>? OnValueChanged;

    public Slider(Action clickSound)
    {
        Style.Width = 200;
        Style.Height = 20;

        OnMouseDown += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                clickSound();
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

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        props.Add($"Text:     \"{Text}\"");
        props.Add($"Value:    {Value:F3}   Step: {Step:F4}");
        return props;
    }

    public void AdjustValue(float delta)
    {
        Value = Math.Clamp(Value + delta, 0f, 1f);
        OnValueChanged?.Invoke(Value);
    }

    private void UpdateValueFromMouse(int mouseX)
    {
        float relativeX = mouseX - ScreenX;
        Value = Math.Clamp(relativeX / ComputedWidth, 0f, 1f);
        OnValueChanged?.Invoke(Value);
    }

    public override void Render(UIRenderer renderer)
    {
        TextureHandle texture = renderer.TextureManager.GetTextureId("/gui/gui.png");

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
