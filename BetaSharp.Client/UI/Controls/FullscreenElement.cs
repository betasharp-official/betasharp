using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Controls;

public abstract class FullscreenElement : UIElement
{
    protected FullscreenElement()
    {
        Style.Position = PositionType.Absolute;
        Style.Top = 0;
        Style.Left = 0;
        Style.Right = -1;
        Style.Bottom = -1;
    }
}
