using BetaSharp.Client.Debug;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.InGame;

public class NewDebugComponentScreen(BetaSharp game, DebugEditorScreen parent) : UIScreen(game)
{
    private Type? _selectedType;
    private ScrollView _scroll = null!;
    private Button _leftButton = null!;
    private Button _rightButton = null!;

    protected override void Init()
    {
        Root.Style.SetPadding(20);
        Root.Style.AlignItems = Align.Center;

        Root.AddChild(new Background(Game.world != null ? BackgroundType.World : BackgroundType.Dirt));

        var title = new Label
        {
            Text = "Add Debug Component",
            Scale = 1.5f,
            TextColor = Color.White
        };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        _scroll = new ScrollView();
        _scroll.Style.Width = 300;
        _scroll.Style.FlexGrow = 1;
        _scroll.Style.MarginBottom = 10;
        Root.AddChild(_scroll);

        foreach (Type componentType in DebugComponents.RegisteredComponentTypes)
        {
            var item = new DebugComponentTypeListItem(componentType);
            item.OnClick += (_) =>
            {
                _selectedType = componentType;
                RefreshList();
                UpdateButtons();
            };
            _scroll.AddContent(item);
        }

        var buttonContainer = new UIElement();
        buttonContainer.Style.FlexDirection = FlexDirection.Row;
        buttonContainer.Style.JustifyContent = Justify.Center;
        buttonContainer.Style.Width = 320;
        Root.AddChild(buttonContainer);

        _leftButton = new Button
        {
            Text = "Add to Left",
            Enabled = false
        };
        _leftButton.Style.Width = 100;
        _leftButton.Style.SetMargin(2);
        _leftButton.OnClick += (_) => AddComponent(false);
        buttonContainer.AddChild(_leftButton);

        _rightButton = new Button
        {
            Text = "Add to Right",
            Enabled = false
        };
        _rightButton.Style.Width = 100;
        _rightButton.Style.SetMargin(2);
        _rightButton.OnClick += (_) => AddComponent(true);
        buttonContainer.AddChild(_rightButton);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Enabled = true
        };
        cancelButton.Style.Width = 100;
        cancelButton.Style.SetMargin(2);
        cancelButton.OnClick += (_) => Game.displayGuiScreen(parent);
        buttonContainer.AddChild(cancelButton);
    }

    private void RefreshList()
    {
        foreach (UIElement child in _scroll.ContentContainer.Children)
        {
            if (child is DebugComponentTypeListItem item)
            {
                item.IsSelected = (item.ComponentType == _selectedType);
            }
        }
    }

    private void UpdateButtons()
    {
        _leftButton.Enabled = _selectedType != null;
        _rightButton.Enabled = _selectedType != null;
    }

    private void AddComponent(bool right)
    {
        if (_selectedType != null)
        {
            var instance = (DebugComponent)Activator.CreateInstance(_selectedType)!;
            instance.Right = right;
            parent.AddComponent(instance);
            Game.displayGuiScreen(parent);
        }
    }
}
