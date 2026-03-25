using BetaSharp.Client.Guis.Layout;

namespace BetaSharp.Client.Guis.Controls;

public class SplitPanel : Control
{
    private const int SplitterMargin = 1;
    public static readonly Property<Side> SideProperty = new();
    public static void SetSide(Control control, Side side) => control.SetProperty(SideProperty, side);

    private bool _mousePressedOnSplitter;
    private int _mousePressXOffset;
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public int SplitterSize { get; set; } = 4;
    public double SplitterPosition { get; set; } = 0.5;
    public uint SplitterColor { get; set; } = 0xFF808080;

    public SplitPanel(int x, int y, int width, int height) : base(x, y, width, height)
    {
    }

    protected override Point GetChildPositionOffset(Control child)
    {
        child.TryGetProperty(SideProperty, out Side side);

        int splitterOffset = side == Side.Start
            ? 0
            : SplitterOffset + SplitterSize / 2;

        return Orientation switch
        {
            Orientation.Vertical => new(Padding.Left, splitterOffset + Padding.Top),
            Orientation.Horizontal => new(splitterOffset + Padding.Left, Padding.Top),
            _ => throw new InvalidOperationException("Invalid SplitPanel orientation"),
        };
    }

    protected override Size GetChildLayoutBounds(Control child)
    {
        child.TryGetProperty(SideProperty, out Side side);

        int splitterOffset = SplitterOffset;

        return Orientation switch
        {
            Orientation.Vertical =>
                side == Side.Start
                    ? new(EffectiveWidth - Padding.Horizontal, splitterOffset - SplitterSize / 2 - Padding.Vertical)
                    : new(EffectiveWidth - Padding.Horizontal, EffectiveHeight - splitterOffset - SplitterSize / 2 - Padding.Vertical),
            Orientation.Horizontal =>
                side == Side.Start
                    ? new(splitterOffset - SplitterSize / 2 - Padding.Horizontal, EffectiveHeight - Padding.Vertical)
                    : new(EffectiveWidth - splitterOffset - SplitterSize / 2 - Padding.Horizontal, EffectiveHeight - Padding.Vertical),
            _ => throw new InvalidOperationException("Invalid SplitPanel orientation"),
        };
    }

    protected override void OnRender(RenderEventArgs e)
    {
        int splitterOffset = SplitterOffset;
        int splitterX;
        int splitterY;
        int splitterWidth;
        int splitterHeight;

        if (Orientation == Orientation.Vertical)
        {
            splitterX = SplitterMargin + Padding.Left;
            splitterY = splitterOffset - SplitterSize / 2 + 1;
            splitterWidth = EffectiveWidth - SplitterMargin * 2 - Padding.Horizontal;
            splitterHeight = SplitterSize - 2;
        }
        else
        {
            splitterX = splitterOffset - SplitterSize / 2 + 1;
            splitterY = SplitterMargin + Padding.Top;
            splitterWidth = SplitterSize - 2;
            splitterHeight = EffectiveHeight - SplitterMargin * 2 - Padding.Vertical;
        }

        Gui.DrawRect(splitterX, splitterY, splitterX + splitterWidth, splitterY + splitterHeight, SplitterColor);
    }

    protected override void OnMousePress(MouseEventArgs e)
    {
        _mousePressedOnSplitter = IsPointOnSplitter(e.X, e.Y);
        _mousePressXOffset = _mousePressedOnSplitter
            ? (Orientation == Orientation.Vertical ? e.Y : e.X) - SplitterOffset
            : 0;
    }

    protected override void OnMouseRelease(MouseEventArgs e)
    {
        _mousePressedOnSplitter = false;
        _mousePressXOffset = 0;
    }

    protected override void OnMouseDrag(MouseEventArgs e)
    {
        if (_mousePressedOnSplitter)
        {
            int totalSize = Orientation == Orientation.Vertical ? EffectiveHeight : EffectiveWidth;
            SplitterPosition = ((Orientation == Orientation.Vertical ? e.Y : e.X) - _mousePressXOffset) / (double)totalSize;
            SplitterPosition = Math.Clamp(SplitterPosition, 0.1, 0.9);
            DoLayoutChildren();
        }
    }

    private bool IsPointOnSplitter(int mouseX, int mouseY)
    {
        int splitterOffset = SplitterOffset;
        int splitterX;
        int splitterY;
        int splitterWidth;
        int splitterHeight;

        if (Orientation == Orientation.Vertical)
        {
            splitterX = SplitterMargin + Padding.Left;
            splitterY = splitterOffset - SplitterSize / 2;
            splitterWidth = EffectiveWidth - SplitterMargin * 2 - Padding.Horizontal;
            splitterHeight = SplitterSize;
        }
        else
        {
            splitterX = splitterOffset - SplitterSize / 2;
            splitterY = SplitterMargin + Padding.Top;
            splitterWidth = SplitterSize;
            splitterHeight = EffectiveHeight - SplitterMargin * 2 - Padding.Vertical;
        }

        return mouseX >= splitterX && mouseX <= splitterX + splitterWidth &&
               mouseY >= splitterY && mouseY <= splitterY + splitterHeight;
    }

    private int SplitterOffset => (int)(SplitterPosition * (Orientation == Orientation.Vertical ? EffectiveHeight : EffectiveWidth));
}
