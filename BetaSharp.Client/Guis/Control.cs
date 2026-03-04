using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis;

public partial class Control
{
    private bool _pressedInside;

    private Control? _parent
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
        }
    }
    private float _xRatio;
    private float _yRatio;
    protected int? TabIndex;
    protected float ZLevel;
    protected readonly List<Control> Children = [];

    public Point AbsolutePosition
    {
        get
        {
            if (_parent == null) return Position;
            Point parentAbsPos = _parent.AbsolutePosition;
            return new(parentAbsPos.X + X, parentAbsPos.Y + Y);
        }
    }

    /// <summary>
    /// Gets the Screen that contains this control, or null if no ancestor is a Screen.
    /// </summary>
    public Screen? ParentScreen
    {
        get
        {
            Control? current = this;
            while (current != null)
            {
                if (current is Screen screen) return screen;
                current = current._parent;
            }
            return null;
        }
    }

    public bool Enabled = true;
    public bool Focused
    {
        get;
        set;
    }
    public string Text
    {
        get;
        set
        {
            field = value;
            DoTextChanged(new TextEventArgs(value));
        }
    } = string.Empty;
    public Size Size
    {
        get;
        set
        {
            Size oldSize = field;
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
            LayoutChildren(oldSize);
        }
    }
    public int Width => Size.Width;
    public int Height => Size.Height;
    private bool _updatingPosition;
    public Point Position
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
            LayoutChildren(Size);
        }
    }
    public int X => Position.X;
    public int Y => Position.Y;
    public List<Control> Descendants => Children.SelectMany(c => c.Descendants).Prepend(this).ToList();

    public virtual bool Focusable => false;
    public virtual bool TopLevel => false;
    public bool Visible = true;

    public Anchors Anchor
    {
        get;
        set
        {
            field = value;
            UpdateAnchorInfo();
            LayoutChildren(Size);
        }
    }
    public CenteringBehavior VerticalCenteringBehavior { get; init; } = CenteringBehavior.Start;
    public CenteringBehavior HorizontalCenteringBehavior { get; init; } = CenteringBehavior.Middle;
    private AnchorInfo _anchorInfo;

    public Control(int x, int y, int width, int height)
    {
        Position = new(x, y);
        Size = new(width, height);
        Visible = true;
    }
    public Control() { }

    private void UpdateAnchorInfo()
    {
        _anchorInfo = new()
        {
            Left = X, Top = Y, Right = X + Width, Bottom = Y + Height,
        };
    }
    private void UpdateRatios()
    {
        if (_parent == null)
        {
            _xRatio = 0;
            _yRatio = 0;
        }
        else if (!_updatingPosition)
        {
            if (!Anchor.HasFlag(Anchors.Left) && !Anchor.HasFlag(Anchors.Right))
            {
                float pos = HorizontalCenteringBehavior switch
                {
                    CenteringBehavior.Start => X,
                    CenteringBehavior.Middle => X + Width / 2f,
                    CenteringBehavior.End => X + Width,
                    _ => throw new InvalidOperationException("Invalid HorizontalCenteringBehavior value"),
                };
                _xRatio = pos / _parent.Width;
            }
            if (!Anchor.HasFlag(Anchors.Top) && !Anchor.HasFlag(Anchors.Bottom))
            {
                float pos = VerticalCenteringBehavior switch
                {
                    CenteringBehavior.Start => Y,
                    CenteringBehavior.Middle => Y + Height / 2f,
                    CenteringBehavior.End => Y + Height,
                    _ => throw new InvalidOperationException("Invalid VerticalCenteringBehavior value"),
                };
                _yRatio = pos / _parent.Height;
            }
        }
    }

    protected void LayoutChildren(Size oldSize)
    {
        foreach (Control child in Children.ToArray())
        {
            child.UpdatePosition(oldSize);
        }
    }

    protected void UpdatePosition(Size oldParentSize)
    {
        if (_parent == null) return;

        int parentWidth = _parent.Width;
        int parentHeight = _parent.Height;

        int newX = X;
        int newY = Y;
        int newWidth = Width;
        int newHeight = Height;

        if (Anchor.HasFlag(Anchors.Left) && Anchor.HasFlag(Anchors.Right))
        {
            newWidth = parentWidth - _anchorInfo.Left - (parentWidth - _anchorInfo.Right);
        }
        else if (Anchor.HasFlag(Anchors.Right))
        {
            newX = parentWidth - _anchorInfo.Right;
        }
        else if (!Anchor.HasFlag(Anchors.Left))
        {
            float offset = HorizontalCenteringBehavior switch
            {
                CenteringBehavior.Start => 0,
                CenteringBehavior.Middle => Width / 2f,
                CenteringBehavior.End => Width,
                _ => throw new InvalidOperationException("Invalid HorizontalCenteringBehavior value"),
            };
            newX = (int)Math.Ceiling(parentWidth * _xRatio - offset);
        }

        if (Anchor.HasFlag(Anchors.Top) && Anchor.HasFlag(Anchors.Bottom))
        {
            newHeight = parentHeight - _anchorInfo.Top - (parentHeight - _anchorInfo.Bottom);
        }
        else if (Anchor.HasFlag(Anchors.Bottom))
        {
            newY = parentHeight - _anchorInfo.Bottom;
        }
        else if (!Anchor.HasFlag(Anchors.Top))
        {
            float offset = VerticalCenteringBehavior switch
            {
                CenteringBehavior.Start => 0,
                CenteringBehavior.Middle => Height / 2f,
                CenteringBehavior.End => Height,
                _ => throw new InvalidOperationException("Invalid VerticalCenteringBehavior value"),
            };
            newY = (int)Math.Floor(parentHeight * _yRatio - offset);
        }

        bool wasUpdating = _updatingPosition;
        _updatingPosition = true;
        Position = new(newX, newY);
        Size = new(newWidth, newHeight);
        _updatingPosition = wasUpdating;
    }

    public void DrawTextureRegion(int x, int y, int u, int v, int width, int height)
    {
        float f = 0.00390625F;
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(x + 0, y + height, ZLevel, (double)((u + 0) * f), (double)((v + height) * f));
        tess.addVertexWithUV(x + width, y + height, ZLevel, (double)((u + width) * f), (double)((v + height) * f));
        tess.addVertexWithUV(x + width, y + 0, ZLevel, (double)((u + width) * f), (double)((v + 0) * f));
        tess.addVertexWithUV(x + 0, y + 0, ZLevel, (double)((u + 0) * f), (double)((v + 0) * f));
        tess.draw();
    }

    /// <summary>
    /// Returns all focusable controls in the order they should be navigated when pressing Tab.
    /// Explicitly set TabIndex values are navigated first, followed by controls without a TabIndex,
    /// which will be navigated in reading order.
    /// </summary>
    public IEnumerable<Control> GetTabOrder()
    {
        return Descendants
            .Where(c => c is { Focusable: true, Visible: true, Enabled: true } && (c.TabIndex ?? 0) >= 0)
            .OrderBy(c => c.TabIndex ?? int.MaxValue)  // Explicit values navigated first
            .ThenBy(c => c.AbsolutePosition.Y)         // Remaining are sorted top-to-bottom
            .ThenBy(c => c.AbsolutePosition.X);        // Then left-to-right
    }


    public bool PointInBounds(int x, int y)
    {
        Point abs = AbsolutePosition;

        // First, check if point is within this control's bounds
        if (!Visible ||
            x < abs.X || y < abs.Y ||
            x >= abs.X + Width || y >= abs.Y + Height)
        {
            return false;
        }

        // Check if parent clips us
        if (_parent != null && !_parent.ContainsPoint(x, y))
        {
            return false;
        }

        // Point is in our bounds - but is it covered by a VISIBLE and ENABLED child?
        foreach (var child in Children)
        {
            if (child is { Visible: true } && child.ContainsPoint(x, y))
            {
                return false;  // Point is in an active child, not in "us"
            }
        }

        return true;
    }

    public virtual bool ContainsPoint(int x, int y)
    {
        Point abs = AbsolutePosition;
        return x >= abs.X && y >= abs.Y &&
               x < abs.X + Width && y < abs.Y + Height;
    }

    public virtual void HandleMouseInput()
    {
        if (!Enabled || !Visible) return;

        var mc = Minecraft.INSTANCE;
        int button = Mouse.getEventButton();
        int clicks = Mouse.getEventClickCount();
        bool isButtonDown = Mouse.getEventButtonState();
        ScaledResolution scaling = new(mc.options, mc.displayWidth, mc.displayHeight);
        int scaledWidth = scaling.ScaledWidth;
        int scaledHeight = scaling.ScaledHeight;
        int windowX = Mouse.getEventX();
        int windowY = Mouse.getEventY();
        int mouseX = windowX * scaledWidth / mc.displayWidth;
        int mouseY = scaledHeight - windowY * scaledHeight / mc.displayHeight - 1;
        float pixelX = (float)windowX * scaledWidth / mc.displayWidth;
        float pixelY = scaledHeight - (float)windowY * scaledHeight / mc.displayHeight - 1;

        foreach (Control child in Children.ToArray())
        {
            child.HandleMouseInput();
        }

        var mouseArgs = new MouseEventArgs(mouseX, mouseY, pixelX, pixelY, button, clicks, isButtonDown);

        if (isButtonDown && button is >= 0 and < Mouse.MouseButtons && PointInBounds(mouseX, mouseY))
        {
            DoMousePress(mouseArgs);
            if (mouseArgs.Handled) return;
        }
        else if (!isButtonDown && button is >= 0 and < Mouse.MouseButtons && _pressedInside)
        {
            DoMouseRelease(mouseArgs);
            if (mouseArgs.Handled) return;
        }

        if (Mouse.getEventDX() != 0 || Mouse.getEventDX() != 0)
        {
            if (PointInBounds(mouseX, mouseY))
            {
                DoMouseMove(mouseArgs);
            }

            if (_pressedInside)
            {
                DoMouseDrag(mouseArgs);
            }
        }
    }

    public virtual void HandleKeyboardInput()
    {
        int key = Keyboard.getEventKey();
        char keyChar = Keyboard.getEventCharacter();
        bool isKeyDown = Keyboard.getEventKeyState();
        bool isRepeat = Keyboard.isRepeatEvent();

        DoKeyInput(new(key, keyChar, isKeyDown, isRepeat));
    }

    public void AddChild(Control child)
    {
        Children.Add(child);
        child._parent = this;
        child.UpdatePosition(Size);
    }
    public void AddChildren(params ReadOnlySpan<Control> children)
    {
        Children.AddRange(children);
        foreach (Control child in children)
        {
            child._parent = this;
            child.UpdatePosition(Size);
        }
    }
}
