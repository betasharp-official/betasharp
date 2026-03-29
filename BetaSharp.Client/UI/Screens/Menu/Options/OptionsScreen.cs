using BetaSharp.Client.Guis;
using BetaSharp.Client.Options;
using BetaSharp.Client.UI.Controls.Core;

namespace BetaSharp.Client.UI.Screens.Menu.Options;

public class OptionsScreen(UIScreen? parent, GameOptions options) : BaseOptionsScreen(parent, options, "options.title")
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
            Button btn = new() { Text = text };
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

        AddSubButton(translations.TranslateKey("options.video"), () => Game.displayGuiScreen(new VideoSettingsScreen(this, Options)));
        AddSubButton("Audio Settings", () => Game.displayGuiScreen(new AudioSettingsScreen(this, Options)));
        AddSubButton(translations.TranslateKey("options.controls"), () => Game.displayGuiScreen(new AllControlsScreen(this, Options)));
        AddSubButton("Debug Options...", () => Game.displayGuiScreen(new DebugOptionsScreen(this, Options)));

        return list;
    }
}
