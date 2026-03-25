using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public class Label : UIElement
{
    public string Text { get; set; } = "";
    public Color TextColor { get; set; } = Color.White;
    public bool Centered { get; set; } = false;

    public override void Measure(float availableWidth, float availableHeight)
    {
        ComputedWidth = Style.Width ?? BetaSharp.Instance.fontRenderer.GetStringWidth(Text);
        ComputedHeight = Style.Height ?? 8;
    }

    public override void Render(UIRenderer renderer)
    {
        if (Centered)
        {
            renderer.DrawCenteredText(Text, ComputedWidth / 2, ComputedHeight / 2 - 4, TextColor);
        }
        else
        {
            renderer.DrawText(Text, 0, 0, TextColor);
        }

        base.Render(renderer);
    }
}
