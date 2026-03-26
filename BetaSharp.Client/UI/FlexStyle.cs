using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI;

public class FlexStyle
{
    public PositionType Position { get; set; } = PositionType.Relative;

    // Size. Null means auto.
    public float? Width { get; set; }
    public float? Height { get; set; }

    // Background. Null means transparent.
    public Color? BackgroundColor { get; set; }

    // Flexbox
    public FlexDirection FlexDirection { get; set; } = FlexDirection.Column;
    public Align AlignItems { get; set; } = Align.Stretch;
    public Align AlignSelf { get; set; } = Align.Auto;
    public Justify JustifyContent { get; set; } = Justify.FlexStart;
    public Wrap FlexWrap { get; set; } = Wrap.NoWrap;

    public float FlexGrow { get; set; } = 0f;
    public float FlexShrink { get; set; } = 1f;

    // Spacing
    public float MarginTop { get; set; } = 0;
    public float MarginRight { get; set; } = 0;
    public float MarginBottom { get; set; } = 0;
    public float MarginLeft { get; set; } = 0;

    public float PaddingTop { get; set; } = 0;
    public float PaddingRight { get; set; } = 0;
    public float PaddingBottom { get; set; } = 0;
    public float PaddingLeft { get; set; } = 0;

    // Positioning when Absolute
    public float? Top { get; set; }
    public float? Right { get; set; }
    public float? Bottom { get; set; }
    public float? Left { get; set; }

    public void SetMargin(float top, float right, float bottom, float left)
    {
        MarginTop = top; MarginRight = right; MarginBottom = bottom; MarginLeft = left;
    }

    public void SetMargin(float all) => SetMargin(all, all, all, all);

    public void SetPadding(float top, float right, float bottom, float left)
    {
        PaddingTop = top; PaddingRight = right; PaddingBottom = bottom; PaddingLeft = left;
    }

    public void SetPadding(float all) => SetPadding(all, all, all, all);
}
