using BetaSharp.Client.Guis.Layout;
using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;

namespace BetaSharp.Client.Guis.Controls;

public partial class Control
{
    private bool _pressedInside;

    private Control? Parent
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
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
            if (Parent == null) return EffectivePosition;
            Point parentAbsPos = Parent.AbsolutePosition;
            Point renderOffset = Parent.GetChildPositionOffset(this);
            return new(parentAbsPos.X + EffectiveX + renderOffset.X, parentAbsPos.Y + EffectiveY + renderOffset.Y);
        }
    }
    public int AbsX => AbsolutePosition.X;
    public int AbsY => AbsolutePosition.Y;

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
                current = current.Parent;
            }
            return null;
        }
    }

    public uint Background { get; set; } // Transparent by default

    public uint Foreground { get; set; } = 0xFFFFFFFF;

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
            DoTextChanged(new(value));
        }
    } = string.Empty;
    public Size EffectiveSize
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
            DoLayoutChildren();
        }
    }
    public int EffectiveWidth
    {
        get => EffectiveSize.Width;
        set => EffectiveSize = EffectiveSize with { Width = value };
    }
    public int EffectiveHeight
    {
        get => EffectiveSize.Height;
        set => EffectiveSize = EffectiveSize with { Height = value };
    }
    public Size Size
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
            DoLayoutChildren();
        }
    }
    public int Width
    {
        get => Size.Width;
        set => Size = Size with { Width = value };
    }
    public int Height
    {
        get => Size.Height;
        set => Size = Size with { Height = value };
    }

    protected bool UpdatingPosition;
    public Point EffectivePosition
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
            DoLayoutChildren();
        }
    }
    public int EffectiveX
    {
        get => EffectivePosition.X;
        set => EffectivePosition = EffectivePosition with { X = value };
    }
    public int EffectiveY
    {
        get => EffectivePosition.Y;
        set => EffectivePosition = EffectivePosition with { Y = value };
    }
    public Point Position
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdateAnchorInfo();
            DoLayoutChildren();
        }
    }
    public int X
    {
        get => Position.X;
        set => Position = Position with { X = value };
    }
    public int Y
    {
        get => Position.Y;
        set => Position = Position with { Y = value };
    }
    public IReadOnlyList<Control> Descendants => Children.SelectMany(c => c.Descendants).Prepend(this).ToList();
    public IReadOnlyList<Control> ChildControls => Children;

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
            DoLayoutChildren();
        }
    }

    public Dock Dock
    {
        get;
        set
        {
            field = value;
            UpdatePosition();
            DoLayoutChildren();
        }
    }

    public Stretch Stretch
    {
        get;
        set
        {
            field = value;
            UpdatePosition();
            DoLayoutChildren();
        }
    }

    public Thickness Padding
    {
        get;
        set
        {
            field = value;
            DoLayoutChildren();
        }
    }

    public Thickness Margin
    {
        get;
        set
        {
            field = value;
            UpdateRatios();
            UpdatePosition();
            DoLayoutChildren();
        }
    }

    public CenteringBehavior VerticalCenteringBehavior { get; init; } = CenteringBehavior.Start;
    public CenteringBehavior HorizontalCenteringBehavior { get; init; } = CenteringBehavior.Middle;
    private AnchorInfo _anchorInfo;
    protected readonly Dictionary<Property, PropertyValue> Properties = new();

    public Control(int x, int y, int width, int height)
    {
        Position = EffectivePosition = new(x, y);
        Size = EffectiveSize = new(width, height);
        Visible = true;
    }
    public Control() { }

    private void UpdateAnchorInfo()
    {
        if (Parent == null)
        {
            _anchorInfo = new() { Left = EffectiveX, Top = EffectiveY, Right = 0, Bottom = 0 };
        }
        else
        {
            Size layoutBounds = Parent.GetChildLayoutBounds(this);
            _anchorInfo = new()
            {
                Left = EffectiveX,
                Top = EffectiveY,
                Right = layoutBounds.Width - (EffectiveX + EffectiveWidth),
                Bottom = layoutBounds.Height - (EffectiveY + EffectiveHeight),
            };
        }
    }
    private void UpdateRatios()
    {
        if (Parent == null)
        {
            _xRatio = 0;
            _yRatio = 0;
        }
        else if (!UpdatingPosition)
        {
            if (Text == "Singleplayer")
            {

            }
            Size layoutBounds = Parent.GetChildLayoutBounds(this);
            if (!Anchor.HasFlag(Anchors.Left) && !Anchor.HasFlag(Anchors.Right))
            {
                float pos = HorizontalCenteringBehavior switch
                {
                    CenteringBehavior.Start => X,
                    CenteringBehavior.Middle => X + Width / 2f,
                    CenteringBehavior.End => X + Width,
                    _ => throw new InvalidOperationException("Invalid HorizontalCenteringBehavior value"),
                };
                _xRatio = pos / (layoutBounds.Width);
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
                _yRatio = pos / (layoutBounds.Height);
            }
        }
    }

    protected void UpdatePosition()
    {
        if (Parent == null) return;

        Size layoutBounds = Parent.GetChildLayoutBounds(this);
        int layoutWidth = layoutBounds.Width;
        int layoutHeight = layoutBounds.Height;

        int newX = X;
        int newY = Y;
        int newWidth = Width;
        int newHeight = Height;

        if (Dock == Dock.None)
        {
            if (Stretch is Stretch.None or Stretch.Vertical)
            {
                if (Anchor.HasFlag(Anchors.Left) && Anchor.HasFlag(Anchors.Right))
                {
                    newWidth = layoutWidth - _anchorInfo.Left - _anchorInfo.Right;
                }
                else if (Anchor.HasFlag(Anchors.Right))
                {
                    newX = layoutWidth - _anchorInfo.Right - Width;
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
                    newX = (int)Math.Round(layoutWidth * _xRatio - offset);
                }
            }
            else if (Stretch is Stretch.Horizontal or Stretch.Fill)
            {
                newX = 0;
                newWidth = layoutWidth;
            }
            else if (Stretch == Stretch.UniformHorizontal)
            {
                newX = 0;
                newWidth = layoutWidth;
            }

            if (Stretch is Stretch.None or Stretch.Horizontal)
            {
                if (Anchor.HasFlag(Anchors.Top) && Anchor.HasFlag(Anchors.Bottom))
                {
                    newHeight = layoutHeight - _anchorInfo.Top - _anchorInfo.Bottom;
                }
                else if (Anchor.HasFlag(Anchors.Bottom))
                {
                    newY = layoutHeight - _anchorInfo.Bottom - Height;
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
                    newY = (int)Math.Round(layoutHeight * _yRatio - offset);
                }
            }
            else if (Stretch is Stretch.Vertical or Stretch.Fill)
            {
                newY = 0;
                newHeight = layoutHeight;
            }
        }
        else
        {
            switch (Dock)
            {
                case Dock.Top:
                    newY = 0;
                    newX = 0;
                    newWidth = layoutWidth;
                    break;
                case Dock.Bottom:
                    newY = layoutHeight - Height;
                    newX = 0;
                    newWidth = layoutWidth;
                    break;
                case Dock.Left:
                    newX = 0;
                    newY = 0;
                    newHeight = layoutHeight;
                    break;
                case Dock.Right:
                    newX = layoutWidth - Width;
                    newY = 0;
                    newHeight = layoutHeight;
                    break;
                case Dock.Fill:
                    newX = 0;
                    newY = 0;
                    newWidth = layoutWidth;
                    newHeight = layoutHeight;
                    break;
                case Dock.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        bool wasUpdating = UpdatingPosition;
        UpdatingPosition = true;
        if (Text == "Singleplayer")
        {

        }
        EffectivePosition = new(newX, newY);
        EffectiveSize = new(newWidth, newHeight);
        UpdatingPosition = wasUpdating;

        DoLayoutChildren();
    }

    public void RecalculateLayoutData()
    {
        UpdateRatios();
        UpdateAnchorInfo();
    }

    protected void DoLayoutChildren()
    {
        if (UpdatingPosition)
        {
            return;
        }

        BeforeLayoutChildren();

        foreach (Control child in Children.ToArray())
        {
            child.UpdatePosition();
        }

        AfterLayoutChildren();
    }

    protected virtual void BeforeLayoutChildren()
    {
    }

    protected virtual void AfterLayoutChildren()
    {
    }

    protected virtual Size GetChildLayoutBounds(Control child)
    {
        return new(EffectiveWidth - Padding.Left - Padding.Right, EffectiveHeight - Padding.Top - Padding.Bottom);
    }

    protected virtual Point GetChildPositionOffset(Control child)
    {
        return new(Padding.Left, Padding.Right);
    }

    public void DrawTextureRegion(int x, int y, int u, int v, int width, int height)
    {
        const float atlasScale = 1/256f;
        Tessellator tess = Tessellator.instance;
        tess.startDrawingQuads();
        tess.addVertexWithUV(x + 0,     y + height, ZLevel, (u + 0)     * atlasScale, (v + height) * atlasScale);
        tess.addVertexWithUV(x + width, y + height, ZLevel, (u + width) * atlasScale, (v + height) * atlasScale);
        tess.addVertexWithUV(x + width, y + 0,      ZLevel, (u + width) * atlasScale, (v + 0)      * atlasScale);
        tess.addVertexWithUV(x + 0,     y + 0,      ZLevel, (u + 0)     * atlasScale, (v + 0)      * atlasScale);
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
            .ThenBy(c => c.AbsY)         // Remaining are sorted top-to-bottom
            .ThenBy(c => c.AbsX);        // Then left-to-right
    }

    public bool PointInBounds(int x, int y)
    {
        if (!Visible || !ContainsPoint(x, y))
        {
            return false;
        }

        if (Parent != null && !Parent.ContainsPoint(x, y))
        {
            return false;
        }

        // Make sure the point isn't inside one of our children
        foreach (var child in Children)
        {
            if (child is { Visible: true } && child.ContainsPoint(x, y))
            {
                return false;
            }
        }

        return true;
    }

    public virtual Rect GetDevToolsHighlightAt(Point point)
    {
        return new(AbsX, AbsY, EffectiveWidth, EffectiveHeight);
    }

    public virtual bool ContainsPoint(int x, int y)
    {
        Point abs = AbsolutePosition;
        return x >= abs.X && y >= abs.Y &&
               x < abs.X + EffectiveWidth && y < abs.Y + EffectiveHeight;
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

    protected virtual void RenderBackground(RenderEventArgs e)
    {
        if (Background != 0)
        {
            Gui.DrawRect(0, 0, EffectiveWidth, EffectiveHeight, Background);
        }
    }

    protected virtual void RenderChildren(RenderEventArgs e)
    {
        foreach (Control child in Children.ToArray())
        {
            child.DoRender(e);
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

    protected virtual void BeforeChildParentSet(Control imminentChild)
    {

    }

    public void AddChild(Control child)
    {
        Children.Add(child);
        BeforeChildParentSet(child);
        child.Parent = this;
        DoLayoutChildren();
        DevToolsWindow.ControlAdded(this, child);
    }
    public void AddChildren(params ReadOnlySpan<Control> children)
    {
        Children.AddRange(children);
        foreach (Control child in children)
        {
            BeforeChildParentSet(child);
            child.Parent = this;
        }
        DoLayoutChildren();
        foreach (Control child in children) DevToolsWindow.ControlAdded(this, child);
    }
    public void InsertChild(int index, Control child)
    {
        Children.Insert(index, child);
        BeforeChildParentSet(child);
        child.Parent = this;
        DoLayoutChildren();
        DevToolsWindow.ControlAdded(this, child);
    }
    public void InsertChildren(int index, params ReadOnlySpan<Control> children)
    {
        Children.InsertRange(index, children);
        foreach (Control child in children)
        {
            BeforeChildParentSet(child);
            child.Parent = this;
        }
        DoLayoutChildren();
        foreach (Control child in children) DevToolsWindow.ControlAdded(this, child);
    }
    public void RemoveChild(Control child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
            DoLayoutChildren();
            DevToolsWindow.ControlRemoved(this, child);
        }
    }
    public void RemoveChildAt(int index)
    {
        Control child = Children[index];
        Children.RemoveAt(index);
        child.Parent = null;
        DoLayoutChildren();
        DevToolsWindow.ControlRemoved(this, child);
    }
    public void ClearChildren()
    {
        foreach (var child in Children)
        {
            child.Parent = null;
        }
        if (DevToolsWindow.IsOpen)
        {
            List<Control> childrenCopy = new(Children);
            Children.Clear();
            DoLayoutChildren();
            foreach (Control child in childrenCopy) DevToolsWindow.ControlRemoved(this, child);
        }
        else
        {
            Children.Clear();
            DoLayoutChildren();
        }
    }

    public bool TryGetProperty<T>(Property<T> property, out T? value)
    {
        if (Properties.TryGetValue(property, out var propValue))
        {
            value = (T?)propValue.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void SetProperty<T>(Property<T> property, T value)
    {
        Properties[property] = new PropertyValue<T>(value);
    }
}
