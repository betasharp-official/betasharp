using BetaSharp.Client.Debug;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.InGame;

public class DebugEditorScreen : UIScreen
{
    private readonly UIScreen? _parentScreen;
    private readonly List<DebugComponent> _components;
    private DebugComponent? _selectedComponent;
    private ScrollView _scroll = null!;
    private Button _changeSideButton = null!;
    private Button _deleteButton = null!;

    public DebugEditorScreen(BetaSharp game, UIScreen? parentScreen) : base(game)
    {
        _parentScreen = parentScreen;
        _components = [];

        foreach (DebugComponent component in game.componentsStorage.Overlay.Components)
        {
            _components.Add(component.Duplicate());
        }
    }

    protected override void Init()
    {
        Root.Style.SetPadding(20);
        Root.Style.AlignItems = Align.Center;

        Root.AddChild(new Background(Game.world != null ? BackgroundType.World : BackgroundType.Dirt));

        var title = new Label
        {
            Text = "Edit Debug Overlay",
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

        RefreshList();

        var buttonContainer = new UIElement();
        buttonContainer.Style.FlexDirection = FlexDirection.Row;
        buttonContainer.Style.FlexWrap = Wrap.Wrap;
        buttonContainer.Style.JustifyContent = Justify.Center;
        buttonContainer.Style.Width = 320;
        Root.AddChild(buttonContainer);

        var addButton = new Button
        {
            Text = "Add New...",
            Enabled = true
        };
        addButton.Style.Width = 150;
        addButton.Style.SetMargin(2);
        addButton.OnClick += (_) => Game.displayGuiScreen(new NewDebugComponentScreen(Game, this));
        buttonContainer.AddChild(addButton);

        var saveButton = new Button
        {
            Text = "Save",
            Enabled = true
        };
        saveButton.Style.Width = 150;
        saveButton.Style.SetMargin(2);
        saveButton.OnClick += (_) =>
        {
            Game.componentsStorage.Overlay.Components.Clear();
            foreach (DebugComponent comp in _components)
            {
                Game.componentsStorage.Overlay.Components.Add(comp.Duplicate());
            }
            Game.componentsStorage.SaveComponents();
            Close();
        };
        buttonContainer.AddChild(saveButton);

        _changeSideButton = new Button
        {
            Text = "Change Side",
            Enabled = false
        };
        _changeSideButton.Style.Width = 100;
        _changeSideButton.Style.SetMargin(2);
        _changeSideButton.OnClick += (_) =>
        {
            if (_selectedComponent != null)
            {
                _selectedComponent.Right = !_selectedComponent.Right;
                RefreshList();
            }
        };
        buttonContainer.AddChild(_changeSideButton);

        _deleteButton = new Button
        {
            Text = "Delete",
            Enabled = false
        };
        _deleteButton.Style.Width = 100;
        _deleteButton.Style.SetMargin(2);
        _deleteButton.OnClick += (_) =>
        {
            if (_selectedComponent != null)
            {
                _components.Remove(_selectedComponent);
                _selectedComponent = null;
                RefreshList();
                UpdateButtons();
            }
        };
        buttonContainer.AddChild(_deleteButton);

        var defaultsButton = new Button
        {
            Text = "Defaults",
            Enabled = true
        };
        defaultsButton.Style.Width = 100;
        defaultsButton.Style.SetMargin(2);
        defaultsButton.OnClick += (_) =>
        {
            _components.Clear();
            DebugComponentsStorage.DefaultComponents(_components);
            _selectedComponent = null;
            RefreshList();
            UpdateButtons();
        };
        buttonContainer.AddChild(defaultsButton);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Enabled = true
        };
        cancelButton.Style.Width = 310;
        cancelButton.Style.SetMargin(2);
        cancelButton.OnClick += (_) => Close();
        buttonContainer.AddChild(cancelButton);
    }

    private void RefreshList()
    {
        _scroll.ContentContainer.Children.Clear();
        foreach (DebugComponent comp in _components)
        {
            var item = new DebugComponentListItem(comp)
            {
                IsSelected = (comp == _selectedComponent)
            };
            item.OnClick += (_) =>
            {
                _selectedComponent = comp;
                RefreshList();
                UpdateButtons();
            };
            _scroll.AddContent(item);
        }
    }

    private void UpdateButtons()
    {
        _changeSideButton.Enabled = _selectedComponent != null;
        _deleteButton.Enabled = _selectedComponent != null;
    }

    public void AddComponent(DebugComponent comp)
    {
        _components.Add(comp);
        _selectedComponent = comp;
        RefreshList();
        UpdateButtons();
    }

    private void Close()
    {
        if (_parentScreen != null)
        {
            Game.displayGuiScreen(_parentScreen);
        }
        else
        {
            Game.displayGuiScreen(null);
        }
    }
}
