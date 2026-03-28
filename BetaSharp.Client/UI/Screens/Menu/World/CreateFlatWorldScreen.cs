using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Worlds.Gen.Flat;

namespace BetaSharp.Client.UI.Screens.Menu.World;

public class CreateFlatWorldScreen(BetaSharp game, CreateWorldScreen parent, string generatorOptions) : UIScreen(game)
{
    private ScrollView _scrollView = null!;
    private readonly List<FlatLayerListItem> _listItems = [];
    private int _selectedIndex = -1;
    private FlatGeneratorInfo _generatorInfo = FlatGeneratorInfo.CreateFromString(generatorOptions);

    private Button _btnRemove = null!;

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = "Customize Superflat World", TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        _scrollView = new ScrollView();
        _scrollView.Style.Width = 300;
        _scrollView.Style.FlexGrow = 1;
        _scrollView.Style.MarginBottom = 10;
        _scrollView.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        Root.AddChild(_scrollView);

        PopulateLayerList();

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Column;
        buttonPanel.Style.AlignItems = Align.Center;

        Panel row1 = new();
        row1.Style.FlexDirection = FlexDirection.Row;

        _btnRemove = new Button { Text = "Remove Layer" };
        _btnRemove.Style.Width = 150;
        _btnRemove.Style.SetMargin(2);
        _btnRemove.Enabled = false;
        _btnRemove.OnClick += (e) => RemoveSelected();
        row1.AddChild(_btnRemove);

        Button btnPresets = new() { Text = "Presets" };
        btnPresets.Style.Width = 150;
        btnPresets.Style.SetMargin(2);
        btnPresets.OnClick += (e) => Game.displayGuiScreen(new FlatPresetsScreen(Game, this));
        row1.AddChild(btnPresets);

        buttonPanel.AddChild(row1);

        Panel row2 = new();
        row2.Style.FlexDirection = FlexDirection.Row;

        Button btnDone = new() { Text = "Done" };
        btnDone.Style.Width = 150;
        btnDone.Style.SetMargin(2);
        btnDone.OnClick += (e) =>
        {
            parent.GeneratorOptions = _generatorInfo.ToString();
            Game.displayGuiScreen(parent);
        };
        row2.AddChild(btnDone);

        Button btnCancel = new() { Text = "Cancel" };
        btnCancel.Style.Width = 150;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Game.displayGuiScreen(parent);
        row2.AddChild(btnCancel);

        buttonPanel.AddChild(row2);
        Root.AddChild(buttonPanel);
    }

    private void PopulateLayerList()
    {
        _scrollView.ContentContainer.Children.Clear();
        _listItems.Clear();
        _selectedIndex = -1;

        for (int i = 0; i < _generatorInfo.FlatLayers.Count; i++)
        {
            int index = i;
            FlatLayerInfo layer = _generatorInfo.FlatLayers[_generatorInfo.FlatLayers.Count - i - 1];
            var item = new FlatLayerListItem(layer);
            item.OnClick += (e) => SelectItem(index);
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void SelectItem(int index)
    {
        _selectedIndex = index;
        foreach (FlatLayerListItem item in _listItems) item.IsSelected = false;
        if (index >= 0 && index < _listItems.Count) _listItems[index].IsSelected = true;
        _btnRemove.Enabled = _selectedIndex >= 0;
    }

    private void RemoveSelected()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _generatorInfo.FlatLayers.Count)
        {
            _generatorInfo.FlatLayers.RemoveAt(_generatorInfo.FlatLayers.Count - _selectedIndex - 1);
            _generatorInfo.UpdateLayerHeights();
            PopulateLayerList();
            SelectItem(Math.Min(_selectedIndex, _generatorInfo.FlatLayers.Count - 1));
        }
    }

    public string GeneratorOptions
    {
        get => _generatorInfo.ToString();
        set
        {
            _generatorInfo = FlatGeneratorInfo.CreateFromString(value);
            PopulateLayerList();
        }
    }
}
