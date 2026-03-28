using System.Diagnostics;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.ListItems;
using BetaSharp.Client.UI.Layout.Flexbox;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Client.UI.Screens.Menu;

public class TexturePacksScreen(UIScreen? parent) : UIScreen(parent?.Game ?? BetaSharp.Instance)
{
    private readonly ILogger<TexturePacksScreen> _logger = Log.Instance.For<TexturePacksScreen>();
    private readonly UIScreen? _parent = parent;
    private ScrollView _scrollView = null!;
    private readonly List<TexturePackListItem> _listItems = [];
    private int _refreshTimer = 0;
    private string _texturePackFolder = "";

    protected override void Init()
    {
        _texturePackFolder = Path.GetFullPath(Path.Combine(BetaSharp.getBetaSharpDir(), "texturepacks"));

        Root.AddChild(new Background());
        Root.Style.AlignItems = Align.Center;
        Root.Style.SetPadding(20);

        Label title = new()
        {
            Text = TranslationStorage.Instance.TranslateKey("texturePack.title"),
            TextColor = Color.White,
            Centered = true
        };
        title.Style.MarginBottom = 10;
        Root.AddChild(title);

        _scrollView = new ScrollView();
        _scrollView.Style.Width = 320;
        _scrollView.Style.FlexGrow = 1;
        _scrollView.Style.MarginBottom = 10;
        _scrollView.Style.BackgroundColor = Color.BackgroundBlackAlpha;
        Root.AddChild(_scrollView);

        PopulatePackList();

        Label info = new()
        {
            Text = TranslationStorage.Instance.TranslateKey("texturePack.folderInfo"),
            TextColor = Color.GrayA0,
            Centered = true
        };
        info.Style.MarginBottom = 10;
        Root.AddChild(info);

        Panel buttonContainer = new();
        buttonContainer.Style.FlexDirection = FlexDirection.Row;
        buttonContainer.Style.JustifyContent = Justify.Center;
        buttonContainer.Style.Width = 320;

        Button btnOpen = new() { Text = TranslationStorage.Instance.TranslateKey("texturePack.openFolder") };
        btnOpen.Style.Width = 150;
        btnOpen.Style.SetMargin(2);
        btnOpen.OnClick += (e) => OpenFolder();
        buttonContainer.AddChild(btnOpen);

        Button btnDone = new() { Text = TranslationStorage.Instance.TranslateKey("gui.done") };
        btnDone.Style.Width = 150;
        btnDone.Style.SetMargin(2);
        btnDone.OnClick += (e) => OnDone();
        buttonContainer.AddChild(btnDone);

        Root.AddChild(buttonContainer);
    }

    private void PopulatePackList()
    {
        _scrollView.ContentContainer.Children.Clear();
        _listItems.Clear();

        Game.texturePackList.updateAvaliableTexturePacks();
        List<TexturePack> packs = Game.texturePackList.AvailableTexturePacks;
        TexturePack selectedPack = Game.texturePackList.SelectedTexturePack;

        for (int i = 0; i < packs.Count; i++)
        {
            TexturePack pack = packs[i];
            TexturePackListItem item = new(pack)
            {
                IsSelected = (pack == selectedPack)
            };
            item.OnClick += (e) => SelectPack(item);
            _scrollView.AddContent(item);
            _listItems.Add(item);
        }
    }

    private void SelectPack(TexturePackListItem selectedItem)
    {
        foreach (TexturePackListItem item in _listItems)
        {
            item.IsSelected = false;
        }
        selectedItem.IsSelected = true;

        Game.texturePackList.setTexturePack(selectedItem.Value);
        Game.textureManager.Reload();
    }

    private void OpenFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "file://" + _texturePackFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open texture pack folder");
        }
    }

    private void OnDone()
    {
        Game.textureManager.Reload();
        if (_parent != null)
        {
            Game.displayGuiScreen(_parent);
        }
        else
        {
            Game.displayGuiScreen(null);
        }
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        if (_refreshTimer-- <= 0)
        {
            _refreshTimer = 20;

            Game.texturePackList.updateAvaliableTexturePacks();

            List<TexturePack> packs = Game.texturePackList.AvailableTexturePacks;
            if (packs.Count != _listItems.Count)
            {
                PopulatePackList();
            }
            else
            {
                TexturePack selectedPack = Game.texturePackList.SelectedTexturePack;
                foreach (TexturePackListItem item in _listItems)
                {
                    item.IsSelected = item.Value == selectedPack;
                }
            }
        }
    }
}
