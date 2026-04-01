using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Util;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds;
using BetaSharp.Worlds.Core.Systems;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.UI.Screens.Menu.World;

public class CreateWorldScreen(
    UIContext context,
    ISingleplayerHost singleplayerHost) : UIScreen(context)
{
    private bool _moreOptions = false;
    private string _worldName = "New World";
    private string _seed = "";
    private WorldType _selectedWorldType = WorldType.Default;
    public string GeneratorOptions { get; set; } = "";

    private TextField _txfWorldName = null!;
    private TextField _txfSeed = null!;
    private Button _btnWorldType = null!;
    private Button _btnCustomize = null!;

    protected override void Init()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        Root.Children.Clear();
        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;
        Root.Style.SetPadding(20);

        TranslationStorage translations = TranslationStorage.Instance;

        Label title = new() { Text = translations.TranslateKey("selectWorld.create"), TextColor = Color.White };
        title.Style.MarginBottom = 20;
        Root.AddChild(title);

        if (!_moreOptions)
        {
            // --- Default View ---
            Label lName = new() { Text = translations.TranslateKey("selectWorld.enterName"), TextColor = Color.GrayA0 };
            lName.Style.MarginBottom = 4;
            Root.AddChild(lName);

            _txfWorldName = new TextField { Text = _worldName };
            _txfWorldName.Style.MarginBottom = 10;
            _txfWorldName.OnTextChanged += (text) => _worldName = text;
            Root.AddChild(_txfWorldName);
        }
        else
        {
            // --- More Options View ---
            Label lSeed = new() { Text = translations.TranslateKey("selectWorld.enterSeed"), TextColor = Color.GrayA0 };
            lSeed.Style.MarginBottom = 4;
            Root.AddChild(lSeed);

            _txfSeed = new TextField { Text = _seed };
            _txfSeed.Style.MarginBottom = 10;
            _txfSeed.OnTextChanged += (text) => _seed = text;
            Root.AddChild(_txfSeed);

            _btnWorldType = CreateButton();
            _btnWorldType.Text = "World Type: " + _selectedWorldType.DisplayName;
            _btnWorldType.Style.MarginBottom = 4;
            _btnWorldType.OnClick += (e) => Context.Navigator.Navigate(new SelectWorldTypeScreen(Context, this, _selectedWorldType));
            Root.AddChild(_btnWorldType);

            _btnCustomize = CreateButton();
            _btnCustomize.Text = "Customize";
            _btnCustomize.Style.MarginBottom = 10;
            _btnCustomize.Enabled = _selectedWorldType == WorldType.Flat;
            _btnCustomize.OnClick += (e) => Context.Navigator.Navigate(new CreateFlatWorldScreen(Context, this, GeneratorOptions));
            Root.AddChild(_btnCustomize);
        }

        Panel buttonPanel = new();
        buttonPanel.Style.FlexDirection = FlexDirection.Row;
        buttonPanel.Style.FlexWrap = Wrap.Wrap;
        buttonPanel.Style.JustifyContent = Justify.Center;
        buttonPanel.Style.Width = 310;
        buttonPanel.Style.MarginTop = 10;

        Button btnCreate = CreateButton();
        btnCreate.Text = translations.TranslateKey("selectWorld.create");
        btnCreate.Style.Width = 150;
        btnCreate.Style.SetMargin(2);
        btnCreate.OnClick += (e) => DoCreateWorld();
        buttonPanel.AddChild(btnCreate);

        string moreOptionsText = _moreOptions ? "Done" : "More World Options...";
        Button btnToggleMore = CreateButton();
        btnToggleMore.Text = moreOptionsText;
        btnToggleMore.Style.Width = 150;
        btnToggleMore.Style.SetMargin(2);
        btnToggleMore.OnClick += (e) =>
        {
            _moreOptions = !_moreOptions;
            BuildUI();
        };
        buttonPanel.AddChild(btnToggleMore);

        Button btnCancel = CreateButton();
        btnCancel.Text = translations.TranslateKey("gui.cancel");
        btnCancel.Style.Width = 150;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(new WorldScreen(Context, singleplayerHost));
        buttonPanel.AddChild(btnCancel);

        Root.AddChild(buttonPanel);
    }

    public void SetWorldType(WorldType type)
    {
        _selectedWorldType = type;
        BuildUI();
    }

    private void DoCreateWorld()
    {
        long worldSeed = new JavaRandom().NextLong();
        if (!string.IsNullOrEmpty(_seed))
        {
            try
            {
                if (long.TryParse(_seed, out long parsedSeed) && parsedSeed != 0L)
                {
                    worldSeed = parsedSeed;
                }
                else
                {
                    worldSeed = CalculateJavaHash(_seed);
                }
            }
            catch
            {
                worldSeed = CalculateJavaHash(_seed);
            }
        }

        string folderName = _worldName.Trim();
        char[] invalidCharacters = ChatAllowedCharacters.allowedCharactersArray;
        foreach (char c in invalidCharacters)
        {
            folderName = folderName.Replace(c, '_');
        }

        if (string.IsNullOrEmpty(folderName)) folderName = "World";
        folderName = GenerateUnusedFolderName(singleplayerHost.SaveLoader, folderName);

        WorldSettings settings = new(worldSeed, _selectedWorldType, GeneratorOptions);
        singleplayerHost.LoadWorld(folderName, _worldName, settings);
    }

    private static long CalculateJavaHash(string input)
    {
        int hash = 0;
        foreach (char c in input)
        {
            hash = 31 * hash + c;
        }
        return hash;
    }

    private static string GenerateUnusedFolderName(IWorldStorageSource worldStorage, string baseFolderName)
    {
        while (worldStorage.GetProperties(baseFolderName) != null)
        {
            baseFolderName += "-";
        }

        return baseFolderName;
    }
}
