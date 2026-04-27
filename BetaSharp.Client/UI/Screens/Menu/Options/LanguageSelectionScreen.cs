using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class LanguageSelectionScreen(UIContext context, UIScreen? parent) : BaseOptionsScreen(context, parent, "options.videoTitle") {

    protected override IEnumerable<GameOption> GetOptions() => [];

    private readonly List<WorldSaveInfo> _saveList = [];
    private ScrollView _scrollView = null!;
    private readonly List<LanguageListItem> _listItems = [];
    private LanguageListItem? _selectedLanguage = null;

    protected override void Init()
    {
        Root.AddChild(new Background());

        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        TranslationStorage translations = TranslationStorage.Instance;

        Label title = new() { Text = translations.TranslateKey("menu.language"), TextColor = Color.White };
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

        buttonContainer.AddChild(row1);

        Panel row2 = new();
        row2.Style.FlexDirection = FlexDirection.Row;
        row2.Style.JustifyContent = Justify.Center;

        Button btnCancel = CreateButton();
        btnCancel.Text = translations.TranslateKey("gui.done");
        btnCancel.Style.Width = 150;
        btnCancel.Style.SetMargin(2);
        btnCancel.OnClick += (e) => Context.Navigator.Navigate(null);
        row2.AddChild(btnCancel);

        buttonContainer.AddChild(row2);
        Root.AddChild(buttonContainer);
    }

    public override void OnEnter()
    {
        PopulateWorldList();
    }

    private void PopulateWorldList()
    {
        _scrollView.ContentContainer.Children.Clear();
        _listItems.Clear();

        foreach (var lang in AssetManager.Languages)
        {
            LanguageListItem item = new(lang.Value);
            item.OnClick += (e) => SelectListItem(item, lang.Key);
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void SelectListItem(LanguageListItem item, string key)
    {
        Options.Language = key.Split('.')[0];
        Options.SaveOptions();

        if(_selectedLanguage != null)
        {
            _selectedLanguage.IsSelected = false;
        }
        item.IsSelected = true;
        _selectedLanguage = item;
    }
}
