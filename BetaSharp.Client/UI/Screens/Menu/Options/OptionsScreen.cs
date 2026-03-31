using BetaSharp.Client.Debug;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class OptionsScreen(
    UIContext context,
    UIScreen? parent,
    DebugComponentsStorage debugStorage) : BaseOptionsScreen(context, parent, "options.title")
{
    protected override IEnumerable<GameOption> GetOptions() => Options.MainScreenOptions;

    protected override UIElement CreateContent()
    {
        Panel list = CreateVerticalList();

        // Main options list
        foreach (GameOption option in GetOptions())
        {
            UIElement control = CreateControlForOption(option);
            control.Style.MarginTop = 2;
            control.Style.MarginBottom = 2;
            control.Style.Width = 310;
            list.AddChild(control);
        }

        // Separator
        Panel separator = new();
        separator.Style.Width = 310;
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
            btn.Style.Width = 310;
            btn.OnClick += (e) =>
            {
                Options.SaveOptions();
                onClick();
            };
            list.AddChild(btn);
        }

        AddSubButton(translations.TranslateKey("options.video"), () => Context.Navigator.Navigate(new VideoSettingsScreen(Context, this)));
        AddSubButton("Audio Settings", () => Context.Navigator.Navigate(new AudioSettingsScreen(Context, this)));
        AddSubButton(translations.TranslateKey("options.controls"), () => Context.Navigator.Navigate(new AllControlsScreen(Context, this)));
        AddSubButton("Debug Options...", () => Context.Navigator.Navigate(new DebugOptionsScreen(Context, this, debugStorage)));

        return list;
    }
}
