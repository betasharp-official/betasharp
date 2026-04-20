using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Items;
using BetaSharp.Worlds.Gen.Flat;

namespace BetaSharp.Client.UI.Screens.Menu.World;

public class FlatPresetsScreen(UIContext context, CreateFlatWorldScreen parent) : UIScreen(context)
{
    public class PresetItem(string name, string value, int iconId = -1)
    {
        public string Name { get; } = name;
        public string Value { get; } = value;
        public int IconId { get; } = iconId != -1 ? iconId : GetIconIdFromValue(value);
        public int IconMeta { get; } = GetIconMetaFromValue(value);

        private static int GetIconIdFromValue(string value)
        {
            var info = FlatGeneratorInfo.CreateFromString(value);
            return info.FlatLayers.Count > 0 ? info.FlatLayers[^1].FillBlock : Block.GrassBlock.ID;
        }

        private static int GetIconMetaFromValue(string value)
        {
            var info = FlatGeneratorInfo.CreateFromString(value);
            return info.FlatLayers.Count > 0 ? info.FlatLayers[^1].FillBlockMeta : 0;
        }
    }

    public static readonly List<PresetItem> Presets =
    [
        new PresetItem("Classic Flat", "2;7,2x3,2;1;village"),
        new PresetItem("Tunnelers' Dream", "2;7,230x1,5x3,2;1;biome_1,dungeon,decoration,stronghold,mineshaft", Block.Stone.ID),
        new PresetItem("Water World", "2;7,5x1,5x3,5x12,90x9;1;village,biome_1"),
        new PresetItem("Overworld", "2;7,59x1,3x3,2;1;village,biome_1,decoration,stronghold,mineshaft,dungeon,lake,lava_lake", Block.DeadBush.ID),
        new PresetItem("Snowy Kingdom", "2;7,59x1,3x3,2,78;1;village,biome_1"),
        new PresetItem("Bottomless Pit", "2;2x4,3x3,2;1;village,biome_1", Item.Feather.id),
        new PresetItem("Desert", "2;7,3x1,52x24,8x12;1;village,biome_1,decoration,stronghold,mineshaft,dungeon"),
        new PresetItem("Redstone Ready", "2;7,3x1,52x24;1;", Item.Redstone.id)
    ];

    private ScrollView _scrollView = null!;
    private TextField _txfOptions = null!;
    private Button _btnSelect = null!;
    private readonly List<FlatPresetListItem> _listItems = [];

    protected override void Init()
    {
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        Label title = new() { Text = "Select a Preset", TextColor = Color.White };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        Label subtitle = new() { Text = "Share your preset with a friend!", TextColor = Color.GrayA0 };
        subtitle.Style.MarginBottom = 4;
        Root.AddChild(subtitle);

        _txfOptions = new TextField { Text = parent.GeneratorOptions };
        _txfOptions.Style.Width = 300;
        _txfOptions.Style.MarginBottom = 10;
        _txfOptions.MaxLength = 1024;
        _txfOptions.OnTextChanged += (text) => UpdateButtonStatus();
        Root.AddChild(_txfOptions);

        _scrollView = new ScrollView();
        _scrollView.Style.Width = 300;
        _scrollView.Style.FlexGrow = 1;
        _scrollView.Style.MarginBottom = 10;
        _scrollView.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        Root.AddChild(_scrollView);

        PopulatePresetList();

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Row;

        _btnSelect = CreateButton();
        _btnSelect.Text = "Select Preset";
        _btnSelect.Style.Width = 150;
        _btnSelect.Style.SetMargin(2);
        _btnSelect.OnClick += (e) => SelectSelected();
        buttonPanel.AddChild(_btnSelect);

        Button btnCancel = CreateButton();
        btnCancel.Text = "Cancel";
        btnCancel.Style.Width = 150;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(parent);
        buttonPanel.AddChild(btnCancel);

        Root.AddChild(buttonPanel);
        UpdateButtonStatus();
    }

    private void PopulatePresetList()
    {
        _listItems.Clear();
        foreach (PresetItem preset in Presets)
        {
            int index = _listItems.Count;
            var item = new FlatPresetListItem(preset);
            item.OnClick += (e) =>
            {
                _txfOptions.Text = preset.Value;
                foreach (FlatPresetListItem li in _listItems) li.IsSelected = false;
                item.IsSelected = true;
                UpdateButtonStatus();
            };
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void UpdateButtonStatus()
    {
        _btnSelect.Enabled = _txfOptions.Text.Length > 0;
    }

    private void SelectSelected()
    {
        if (_txfOptions.Text.Length > 0)
        {
            parent.GeneratorOptions = _txfOptions.Text;
            Context.Navigator.Navigate(parent);
        }
    }
}
