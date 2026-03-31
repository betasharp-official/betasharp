using BetaSharp.Client.Debug;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class DebugComponentListItem : ListItem
{
    public DebugComponent Component { get; }

    public DebugComponentListItem(DebugComponent component)
    {
        Component = component;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Style.Height = 40;

        var nameLabel = new Label
        {
            Text = DebugComponents.GetName(Component.GetType()),
            Scale = 1.0f,
            TextColor = Color.White,
            IsHitTestVisible = false
        };
        AddChild(nameLabel);

        var sideLabel = new Label
        {
            Text = Component.Right ? "Right" : "Left",
            Scale = 0.8f,
            TextColor = Color.GrayA0,
            IsHitTestVisible = false
        };
        sideLabel.Style.Position = Layout.Flexbox.PositionType.Absolute;
        sideLabel.Style.Right = 10;
        sideLabel.Style.Top = 12;
        AddChild(sideLabel);

        string? desc = DebugComponents.GetDescription(Component.GetType());
        if (!string.IsNullOrEmpty(desc))
        {
            var descLabel = new Label
            {
                Text = desc,
                TextColor = Color.GrayA0,
                Scale = 0.8f,
                IsHitTestVisible = false
            };
            descLabel.Style.MarginTop = 2;
            AddChild(descLabel);
        }
    }

    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);
    }
}
