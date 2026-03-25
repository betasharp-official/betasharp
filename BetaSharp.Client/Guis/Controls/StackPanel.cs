using BetaSharp.Client.Guis.Layout;

namespace BetaSharp.Client.Guis.Controls;

public class StackPanel : Control
{
    public int Gap { get; set; } = 5;
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public StackPanel(int x, int y) : base(x, y, 0, 0)
    {
        Anchor = Anchors.Top | Anchors.Left;
    }

    protected override Point GetChildPositionOffset(Control child)
    {
        int index = Children.IndexOf(child);
        int offset = index * Gap + Children.Take(index).Sum(c => Orientation == Orientation.Vertical ? c.EffectiveHeight : c.EffectiveWidth);
        return Orientation switch
        {
            Orientation.Vertical => new(Padding.Left, offset + Padding.Top),
            Orientation.Horizontal => new(offset + Padding.Left, Padding.Top),
            _ => throw new InvalidOperationException("Invalid StackPanel orientation"),
        };
    }
}
