using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public class Panel : UIElement
{
    public Color BackgroundColor { get; set; } = Color.BackgroundBlackAlpha;

    public override void Render(UIRenderer renderer)
    {
        renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, BackgroundColor);

        base.Render(renderer);
    }
}
