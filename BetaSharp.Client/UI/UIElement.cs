namespace BetaSharp.Client.UI;

public class UIElement
{
    public UIElement? Parent { get; set; }
    public List<UIElement> Children { get; } = [];

    public FlexStyle Style { get; set; } = new FlexStyle();

    // Computed Layout Box
    public float ComputedX { get; set; }
    public float ComputedY { get; set; }
    public float ComputedWidth { get; set; }
    public float ComputedHeight { get; set; }

    public float ScreenX => (Parent?.ScreenX ?? 0) + ComputedX;
    public float ScreenY => (Parent?.ScreenY ?? 0) + ComputedY;

    // Events
    public Action<UIMouseEvent>? OnMouseDown;
    public Action<UIMouseEvent>? OnMouseUp;
    public Action<UIMouseEvent>? OnClick;
    public Action<UIMouseEvent>? OnMouseEnter;
    public Action<UIMouseEvent>? OnMouseLeave;
    public Action<UIMouseEvent>? OnMouseMove;
    public Action<UIMouseEvent>? OnMouseScroll;
    public Action<UIKeyEvent>? OnKeyDown;

    public bool IsHovered { get; internal set; }
    public bool IsFocused { get; internal set; }

    public bool ClipToBounds { get; set; } = false;

    public void AddChild(UIElement child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void RemoveChild(UIElement child)
    {
        if (child.Parent == this)
        {
            child.Parent = null;
            Children.Remove(child);
        }
    }

    public virtual void Measure(float availableWidth, float availableHeight)
    {
        ComputedWidth = Style.Width ?? availableWidth;
        ComputedHeight = Style.Height ?? availableHeight;
    }

    public virtual void Arrange(float x, float y, float width, float height)
    {
        ComputedX = x;
        ComputedY = y;
        ComputedWidth = width;
        ComputedHeight = height;
    }

    public virtual void Update(float partialTicks)
    {
        foreach (UIElement child in Children)
        {
            child.Update(partialTicks);
        }
    }

    public virtual void Render(Rendering.UIRenderer renderer)
    {
        foreach (UIElement child in Children)
        {
            renderer.PushTranslate(child.ComputedX, child.ComputedY);
            child.Render(renderer);
            renderer.PopTranslate(child.ComputedX, child.ComputedY);
        }
    }

    // Fired automatically by the layout engine
    public virtual void OnLayoutApplied()
    {
    }

    public virtual UIElement? HitTest(float screenX, float screenY)
    {
        if (ClipToBounds && !ContainsPoint(screenX, screenY))
        {
            return null;
        }

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UIElement? hit = Children[i].HitTest(screenX, screenY);
            if (hit != null) return hit;
        }

        if (ContainsPoint(screenX, screenY))
        {
            return this;
        }

        return null;
    }

    public bool ContainsPoint(float screenX, float screenY)
    {
        float sx = ScreenX;
        float sy = ScreenY;
        return screenX >= sx && screenX < sx + ComputedWidth &&
               screenY >= sy && screenY < sy + ComputedHeight;
    }
}
