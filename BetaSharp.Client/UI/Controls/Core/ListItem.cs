using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.Core;

public abstract class ListItem : UIElement
{
    public bool IsSelected { get; set; }

    protected ListItem()
    {
        Style.Width = null; // Fill parent
        Style.Height = 32;
        Style.SetPadding(4);
        Style.MarginBottom = 4;
        Style.MarginRight = 10;

        OnMouseEnter += (_) => IsHovered = true;
        OnMouseLeave += (_) => IsHovered = false;
    }

    public override void Render(UIRenderer renderer)
    {
        // Background highlight
        if (IsSelected)
        {
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, Color.WhiteAlpha20);
        }

        // Borders
        if (IsSelected)
        {
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, Color.White);
            renderer.DrawRect(1, 1, ComputedWidth - 2, ComputedHeight - 2, Color.Black);
        }
        else
        {
            Color borderColor = IsHovered ? Color.GrayCC : Color.GrayA0;
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, borderColor);
            renderer.DrawRect(1, 1, ComputedWidth - 2, ComputedHeight - 2, Color.Black);
        }

        // Selection Bar
        if (IsSelected)
        {
            // Shadow behind the bar for depth
            renderer.DrawRect(3, 0, 1, ComputedHeight, Color.BackgroundBlackAlpha);

            renderer.DrawRect(0, 0, 3, ComputedHeight, Color.GrayCC);
            renderer.DrawRect(0, 0, 1, ComputedHeight, Color.White);
            renderer.DrawRect(2, 0, 1, ComputedHeight, Color.Gray80);
            renderer.DrawRect(0, 0, 3, 1, Color.White);
            renderer.DrawRect(0, ComputedHeight - 1, 3, 1, Color.Gray80);
        }

        base.Render(renderer);
    }
}

public class ListItem<T>(T value) : ListItem
{
    public T Value { get; set; } = value;
}
