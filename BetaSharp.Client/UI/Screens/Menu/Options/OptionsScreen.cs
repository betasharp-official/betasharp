using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class OptionsScreen(
    UIContext context,
    UIScreen? parent,
    TexturePacks texturePacks) : BaseOptionsScreen(context, parent, "options.title")
{
    protected override List<OptionSection> GetOptions() => [  ];

    protected override UIElement CreateContent()
    {
        Panel list = CreateTwoColumnList();

        // Main options list
        foreach (GameOption option in Options.MainScreenOptions)
        {
            UIElement control = CreateControlForOption(option);
            control.Style.MarginTop = 2;
            control.Style.MarginBottom = 2;
            control.Style.MarginLeft = 4;
            control.Style.MarginRight = 4;
            control.Style.Width = ButtonSize;
            list.AddChild(control);
        }

        // Separator
        Panel separator = new();
        separator.Style.Width = ScrollContentSize;
        separator.Style.Height = 1;
        separator.Style.BackgroundColor = Color.Gray70;
        separator.Style.MarginTop = 6;
        separator.Style.MarginBottom = 6;
        list.AddChild(separator);

        // Sub-menu buttons
        TranslationStorage translations = TranslationStorage.Instance;

        void AddSubButton(string text, Action onClick)
        {
            Button btn = CreateButton();
            btn.Text = text;
            btn.Style.MarginTop = 2;
            btn.Style.MarginBottom = 2;
            btn.Style.MarginLeft = 4;
            btn.Style.MarginRight = 4;
            btn.Style.Width = ButtonSize;
            btn.OnClick += (e) =>
            {
                Options.SaveOptions();
                onClick();
            };
            list.AddChild(btn);
        }

        AddSubButton(translations.TranslateKey("options.video"), () => Context.Navigator.Navigate(new VideoSettingsScreen(Context, this)));
        AddSubButton("UI Settings", () => Context.Navigator.Navigate(new UISettingsScreen(Context, this)));
        AddSubButton("Audio Settings", () => Context.Navigator.Navigate(new AudioSettingsScreen(Context, this)));
        AddSubButton(translations.TranslateKey("options.controls"), () => Context.Navigator.Navigate(new AllControlsScreen(Context, this)));
        AddSubButton("Texture Packs", () => Context.Navigator.Navigate(new TexturePacksScreen(Context, this, texturePacks)));
        AddSubButton("Credits", () => Context.Navigator.Navigate(new CreditsScreen(Context, this)));

        return list;
    }
}
