using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class Label : UIElement
{
    public string Text { get; set; } = "";
    public Color TextColor { get; set; } = Color.White;
    public bool Centered { get; set; } = false;

    // Scale is rounded to the nearest whole number for pixel art consistency.
    public float Scale
    {
        get;
        set => field = MathF.Round(value);
    } = 1.0f;
    public bool HasShadow { get; set; } = true;

    public override void Measure(float availableWidth, float availableHeight)
    {
        ComputedWidth = (Style.Width ?? BetaSharp.Instance.fontRenderer.GetStringWidth(Text)) * Scale;
        ComputedHeight = (Style.Height ?? 8) * Scale;
    }

    public override void Render(UIRenderer renderer)
    {
        if (Centered)
        {
            renderer.DrawCenteredText(Text, ComputedWidth / 2, ComputedHeight / 2 - 4 * Scale, TextColor, 0, Scale, HasShadow);
        }
        else
        {
            renderer.DrawText(Text, 0, 0, TextColor, Scale, HasShadow);
        }

        base.Render(renderer);
    }
}
