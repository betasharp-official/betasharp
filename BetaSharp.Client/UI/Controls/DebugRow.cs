using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Controls;

public class DebugRow : UIElement
{
    private const int Padding = 2;

    public DebugRow(string text, Color? color = null)
    {
        Style.Height = 10;
        Style.FlexDirection = FlexDirection.Row;
        Style.AlignItems = Align.Center;
        Style.PaddingLeft = Padding;
        Style.PaddingRight = Padding;
        Style.BackgroundColor = new Color(128, 128, 128, 96);

        AddChild(new Label
        {
            Text = text,
            TextColor = color ?? Color.White,
            HasShadow = true,
        });
    }

    public static UIElement Spacer()
    {
        UIElement spacer = new();
        spacer.Style.Height = 10;
        return spacer;
    }
}
