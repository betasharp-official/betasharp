using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Layout;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public class ScrollView : UIElement
{
    public UIElement ContentContainer { get; private set; }

    public float ScrollY { get; set; }
    public float MaxScrollY => Math.Max(0, ContentContainer.ComputedHeight - ComputedHeight);

    private bool _isDraggingScrollbar;
    private bool _isDraggingContent;
    private float _dragStartY;
    private float _dragInitialScrollY;

    public ScrollView()
    {
        ClipToBounds = true;
        ContentContainer = new UIElement();
        ContentContainer.Style.FlexDirection = Layout.Flexbox.FlexDirection.Column;
        ContentContainer.Parent = this;

        OnMouseDown += (e) =>
        {
            if (e.Button == MouseButton.Left)
            {
                float relativeX = e.MouseX - ScreenX;
                float relativeY = e.MouseY - ScreenY;

                if (relativeX >= ComputedWidth - 10)
                {
                    _isDraggingScrollbar = true;
                    _dragStartY = e.MouseY;

                    float viewRatio = Math.Min(1.0f, ComputedHeight / ContentContainer.ComputedHeight);
                    float barHeight = Math.Max(32f, ComputedHeight * viewRatio);
                    float maxBarScroll = ComputedHeight - barHeight;
                    float scrollProgress = MaxScrollY > 0 ? ScrollY / MaxScrollY : 0;

                    _dragInitialScrollY = scrollProgress * maxBarScroll;
                }
                else
                {
                    _isDraggingContent = true;
                    _dragStartY = e.MouseY;
                    _dragInitialScrollY = ScrollY;
                }
            }
        };

        OnMouseMove += (e) =>
        {
            if (_isDraggingScrollbar)
            {
                float dragDelta = e.MouseY - _dragStartY;

                float viewRatio = Math.Min(1.0f, ComputedHeight / ContentContainer.ComputedHeight);
                float barHeight = Math.Max(32f, ComputedHeight * viewRatio);
                float maxBarScroll = ComputedHeight - barHeight;

                if (maxBarScroll > 0)
                {
                    float newThumbY = _dragInitialScrollY + dragDelta;
                    float scrollProgress = Math.Clamp(newThumbY / maxBarScroll, 0, 1);
                    ScrollY = scrollProgress * MaxScrollY;
                    FixContentOffset();
                }
                e.Handled = true;
            }
            else if (_isDraggingContent)
            {
                float dragDelta = e.MouseY - _dragStartY;
                ScrollY = _dragInitialScrollY - dragDelta;
                ScrollY = Math.Clamp(ScrollY, 0, MaxScrollY);
                FixContentOffset();
                e.Handled = true;
            }
        };

        OnMouseUp += (e) =>
        {
            _isDraggingScrollbar = false;
            _isDraggingContent = false;
        };

        OnMouseScroll += (e) =>
        {
            if (MaxScrollY > 0)
            {
                ScrollY -= e.ScrollDelta / 120.0f * 20.0f;
                ScrollY = Math.Clamp(ScrollY, 0, MaxScrollY);
                FixContentOffset();
                e.Handled = true;
            }
        };
    }

    public void ScrollBy(float delta)
    {
        ScrollY = Math.Clamp(ScrollY + delta, 0, MaxScrollY);
        FixContentOffset();
    }

    public void AddContent(UIElement child)
    {
        ContentContainer.AddChild(child);
    }

    public override UIElement? HitTest(float screenX, float screenY)
    {
        if (ClipToBounds && !ContainsPoint(screenX, screenY)) return null;

        UIElement? hitContent = ContentContainer.HitTest(screenX, screenY);
        if (hitContent != null) return hitContent;

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            UIElement? hitChild = Children[i].HitTest(screenX, screenY);
            if (hitChild != null) return hitChild;
        }

        if (ContainsPoint(screenX, screenY)) return this;

        return null;
    }

    public override void OnLayoutApplied(LayoutAppliedContext context)
    {
        base.OnLayoutApplied(context);

        FlexLayout.LayoutContext layoutContext = new()
        {
            Root = ContentContainer,
            AvailableWidth = ComputedWidth - 10,
            AvailableHeight = 999999f,
            MeasureString = context.MeasureString
        };

        FlexLayout.ApplyLayout(layoutContext);

        float calculatedHeight = 0f;
        foreach (UIElement child in ContentContainer.Children)
        {
            calculatedHeight = Math.Max(calculatedHeight, child.ComputedY + child.ComputedHeight + child.Style.MarginBottom);
        }
        ContentContainer.ComputedHeight = calculatedHeight;

        ScrollY = Math.Clamp(ScrollY, 0, MaxScrollY);
        FixContentOffset();
    }

    private void FixContentOffset()
    {
        ContentContainer.Arrange(0, -ScrollY, ComputedWidth - 10, ContentContainer.ComputedHeight);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        ContentContainer.Update(partialTicks);
    }

    public override void Render(UIRenderer renderer)
    {
        renderer.EnableClipping(0, 0, (int)ComputedWidth, (int)ComputedHeight);

        renderer.PushTranslate(ContentContainer.ComputedX, ContentContainer.ComputedY);
        ContentContainer.Render(renderer);
        renderer.PopTranslate();

        renderer.DisableClipping();

        if (MaxScrollY > 0 && ContentContainer.ComputedHeight > 0)
        {
            // track
            renderer.DrawRect(ComputedWidth - 10, 0, 10, ComputedHeight, Color.BlackAlphaC0);

            float viewRatio = Math.Min(1.0f, ComputedHeight / ContentContainer.ComputedHeight);
            float barHeight = Math.Max(32f, ComputedHeight * viewRatio);

            float scrollProgress = ScrollY / MaxScrollY;
            float barY = scrollProgress * (ComputedHeight - barHeight);

            renderer.DrawRect(ComputedWidth - 10, barY, 10, barHeight, Color.Gray80);
            renderer.DrawRect(ComputedWidth - 10, barY, 9, barHeight - 1, new Color(192, 192, 192, 255));
        }
    }
}
