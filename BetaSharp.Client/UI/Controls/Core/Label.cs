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

    public override bool DoTextMeasuring => true;

    public override List<string> GetInspectorProperties()
    {
        List<string> props = base.GetInspectorProperties();
        props.Add($"Text:     \"{Text}\"");
        props.Add($"Color:    #{TextColor}");
        props.Add($"Scale:    {Scale}   Shadow: {HasShadow}   Centered: {Centered}");
        return props;
    }

    public override void Measure(MeasureContext context)
    {
        ComputedWidth = (Style.Width ?? context.MeasureString(Text)) * Scale;
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
