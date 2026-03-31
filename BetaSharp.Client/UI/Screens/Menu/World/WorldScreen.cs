using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.UI.Screens.Menu.World;

public class WorldScreen(
    UIContext context,
    ISingleplayerHost singleplayerHost) : UIScreen(context)
{
    private readonly List<WorldSaveInfo> _saveList = [];
    private ScrollView _scrollView = null!;
    private readonly List<WorldListItem> _listItems = [];
    private int _selectedWorldIndex = -1;

    private Button _btnSelect = null!;
    private Button _btnRename = null!;
    private Button _btnDelete = null!;

    protected override void Init()
    {
        Root.AddChild(new Background());
        LoadSaves();

        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        TranslationStorage translations = TranslationStorage.Instance;

        Label title = new() { Text = translations.TranslateKey("selectWorld.title"), TextColor = Color.White };
        title.Style.MarginBottom = 8;
        Root.AddChild(title);
        AddTitleSpacer();

        _scrollView = new ScrollView();
        _scrollView.Style.Width = 300;
        _scrollView.Style.FlexGrow = 1;
        _scrollView.Style.MaxHeight = 200;
        _scrollView.Style.MarginBottom = 10;
        _scrollView.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        Root.AddChild(_scrollView);

        PopulateWorldList();

        Panel buttonContainer = new();
        buttonContainer.Style.FlexDirection = FlexDirection.Column;
        buttonContainer.Style.AlignItems = Align.Center;
        buttonContainer.Style.Width = 320;

        Panel row1 = new();
        row1.Style.FlexDirection = FlexDirection.Row;
        row1.Style.JustifyContent = Justify.Center;
        row1.Style.MarginBottom = 2;

        _btnSelect = CreateButton();
        _btnSelect.Text = translations.TranslateKey("selectWorld.select");
        _btnSelect.Style.Width = 150;
        _btnSelect.Style.SetMargin(2);
        _btnSelect.OnClick += (e) => SelectWorld(_selectedWorldIndex);
        row1.AddChild(_btnSelect);

        Button btnCreate = CreateButton();
        btnCreate.Text = translations.TranslateKey("selectWorld.create");
        btnCreate.Style.Width = 150;
        btnCreate.Style.SetMargin(2);
        btnCreate.OnClick += (e) => Context.Navigator.Navigate(new CreateWorldScreen(Context, singleplayerHost));
        row1.AddChild(btnCreate);

        buttonContainer.AddChild(row1);

        Panel row2 = new();
        row2.Style.FlexDirection = FlexDirection.Row;
        row2.Style.JustifyContent = Justify.Center;

        _btnRename = CreateButton();
        _btnRename.Text = translations.TranslateKey("selectWorld.rename");
        _btnRename.Style.Width = 72;
        _btnRename.Style.SetMargin(2);
        _btnRename.OnClick += (e) => RenameSelected();
        row2.AddChild(_btnRename);

        _btnDelete = CreateButton();
        _btnDelete.Text = translations.TranslateKey("selectWorld.delete");
        _btnDelete.Style.Width = 72;
        _btnDelete.Style.SetMargin(2);
        _btnDelete.OnClick += (e) => DeleteSelected();
        row2.AddChild(_btnDelete);

        Button btnCancel = CreateButton();
        btnCancel.Text = translations.TranslateKey("gui.cancel");
        btnCancel.Style.Width = 150;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(null);
        row2.AddChild(btnCancel);

        buttonContainer.AddChild(row2);
        Root.AddChild(buttonContainer);

        UpdateButtons();
    }

    public override void OnEnter()
    {
        LoadSaves();
        PopulateWorldList();
        UpdateButtons();
    }

    private void LoadSaves()
    {
        _saveList.Clear();
        _saveList.AddRange(singleplayerHost.SaveLoader.GetAll());
        _saveList.Sort();
        _selectedWorldIndex = -1;
    }

    private void PopulateWorldList()
    {
        _scrollView.ContentContainer.Children.Clear();
        _listItems.Clear();
        _selectedWorldIndex = -1;

        for (int i = 0; i < _saveList.Count; i++)
        {
            int index = i;
            WorldListItem item = new(_saveList[i]);
            item.OnClick += (e) => SelectListItem(index);
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void SelectListItem(int index)
    {
        _selectedWorldIndex = index;
        foreach (WorldListItem item in _listItems) item.IsSelected = false;
        if (index >= 0 && index < _listItems.Count) _listItems[index].IsSelected = true;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = _selectedWorldIndex >= 0;
        bool isSupported = hasSelection && !_saveList[_selectedWorldIndex].IsUnsupported;

        _btnSelect.Enabled = isSupported;
        _btnRename.Enabled = isSupported;
        _btnDelete.Enabled = hasSelection;
    }

    private void SelectWorld(int index)
    {
        if (index < 0 || index >= _saveList.Count) return;
        WorldSaveInfo worldInfo = _saveList[index];
        if (worldInfo.IsUnsupported) return;

        string worldFileName = worldInfo.FileName ?? $"World{index}";
        WorldProperties? props = singleplayerHost.SaveLoader.GetProperties(worldFileName);

        WorldSettings settings;
        if (props != null)
        {
            settings = new WorldSettings(props.RandomSeed, props.TerrainType, props.GeneratorOptions);
        }
        else
        {
            settings = new WorldSettings(0L, WorldType.Default);
        }

        singleplayerHost.LoadWorld(worldFileName, worldInfo.DisplayName, settings);
    }

    private void RenameSelected()
    {
        if (_selectedWorldIndex < 0) return;
        string fileName = _saveList[_selectedWorldIndex].FileName;
        Context.Navigator.Navigate(new RenameWorldScreen(Context, this, fileName, singleplayerHost.SaveLoader));
    }

    private void DeleteSelected()
    {
        if (_selectedWorldIndex < 0) return;
        WorldSaveInfo worldInfo = _saveList[_selectedWorldIndex];

        TranslationStorage translations = TranslationStorage.Instance;
        string deleteQuestion = translations.TranslateKey("selectWorld.deleteQuestion");
        string deleteWarning = "'" + worldInfo.DisplayName + "' " + translations.TranslateKey("selectWorld.deleteWarning");

        Context.Navigator.Navigate(new ConfirmationScreen(Context, this, deleteQuestion, deleteWarning, translations.TranslateKey("selectWorld.deleteButton"), translations.TranslateKey("gui.cancel"), (confirmed) =>
        {
            if (confirmed)
            {
                singleplayerHost.SaveLoader.Flush();
                singleplayerHost.SaveLoader.Delete(worldInfo.FileName);
                LoadSaves();
                PopulateWorldList();
                UpdateButtons();
            }
            Context.Navigator.Navigate(this);
        }));
    }
}
