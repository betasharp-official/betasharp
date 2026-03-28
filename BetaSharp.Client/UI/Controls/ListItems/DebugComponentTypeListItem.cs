using BetaSharp.Client.Debug;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class DebugComponentTypeListItem : ListItem
{
    public Type ComponentType { get; }

    public DebugComponentTypeListItem(Type type)
    {
        ComponentType = type;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Style.Height = 40;

        var nameLabel = new Label
        {
            Text = DebugComponents.GetName(ComponentType),
            Scale = 1.0f,
            TextColor = Color.White,
            IsHitTestVisible = false
        };
        AddChild(nameLabel);

        string? desc = DebugComponents.GetDescription(ComponentType);
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
}
