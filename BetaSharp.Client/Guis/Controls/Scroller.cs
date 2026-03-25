namespace BetaSharp.Client.Guis.Controls;

public class Scroller : Control
{
    private int _scrollOffset;
    private int _contentHeight;

    private int _initialClickY = -1;
    private int _initialClickX = -1;
    private int _initialScrollOffset;
    private bool _mouseHasMovedMuchSinceClick;
    private bool _mouseWasDown;

    private const int DragThreshold = 5;
    private const int ScrollbarWidth = 6;
    private const int ScrollbarLeftMargin = 1;
    private const int MinScrollbarHeight = 32;

    public uint ScrollbarBackgroundColor { get; set; } = 0xC0000000;
    public uint ScrollbarThumbColor { get; set; } = 0xFF808080;
    public uint ScrollbarThumbHighlightColor { get; set; } = 0xFFC0C0C0;

    public Scroller(int x, int y, int width, int height) : base(x, y, width, height)
    {
    }

    protected override void AfterLayoutChildren()
    {
        _contentHeight = 0;
        foreach (var child in Children)
        {
            int childBottom = child.EffectiveY + child.EffectiveHeight;
            if (childBottom > _contentHeight)
            {
                _contentHeight = childBottom;
            }
        }
        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _contentHeight - EffectiveHeight));
    }

    protected override void OnRender(RenderEventArgs e)
    {
        if (_contentHeight > EffectiveHeight)
        {
            int barHeight = Math.Clamp((EffectiveHeight * EffectiveHeight) / _contentHeight, MinScrollbarHeight, EffectiveHeight);
            int scrollRange = _contentHeight - EffectiveHeight;
            int barY = scrollRange > 0 ? (_scrollOffset * (EffectiveHeight - barHeight)) / scrollRange : 0;
            int barX = EffectiveWidth - ScrollbarWidth;

            Gui.DrawRect(barX, 0, barX + ScrollbarWidth, EffectiveHeight, ScrollbarBackgroundColor);
            Gui.DrawRect(barX, barY, barX + ScrollbarWidth, barY + barHeight, ScrollbarThumbColor);
            Gui.DrawRect(barX, barY, barX + ScrollbarWidth - 1, barY + barHeight - 1, ScrollbarThumbHighlightColor);
        }
    }

    protected override void OnPostRender(RenderEventArgs e)
    {
    }

    protected override void OnMousePress(MouseEventArgs e)
    {
        _initialClickY = e.Y;
        _initialClickX = e.X;
        _initialScrollOffset = _scrollOffset;
        _mouseHasMovedMuchSinceClick = false;
        _mouseWasDown = true;
    }

    protected override void OnMouseRelease(MouseEventArgs e)
    {
        _initialClickY = -1;
        _initialClickX = -1;
        _mouseWasDown = false;
    }

    protected override void OnMouseDrag(MouseEventArgs e)
    {
        if (!_mouseWasDown) return;

        int listRight = EffectiveWidth - ScrollbarWidth - ScrollbarLeftMargin;
        int absX = AbsX;
        int localClickX = _initialClickX - absX;

        if (Math.Abs(e.Y - _initialClickY) > DragThreshold ||
            Math.Abs(e.X - _initialClickX) > DragThreshold)
        {
            _mouseHasMovedMuchSinceClick = true;
        }

        if (_mouseHasMovedMuchSinceClick)
        {
            int deltaY = e.Y - _initialClickY;

            if (localClickX >= listRight)
            {
                int scrollRange = _contentHeight - EffectiveHeight;
                if (scrollRange > 0)
                {
                    int barHeight = Math.Clamp((EffectiveHeight * EffectiveHeight) / _contentHeight, MinScrollbarHeight, EffectiveHeight);
                    int trackHeight = EffectiveHeight - barHeight;
                    if (trackHeight > 0)
                    {
                        deltaY = (deltaY * scrollRange) / trackHeight;
                        deltaY *= -1;
                    }
                }
            }

            _scrollOffset = Math.Clamp(_initialScrollOffset - deltaY, 0, Math.Max(0, _contentHeight - EffectiveHeight));
        }
    }
}
