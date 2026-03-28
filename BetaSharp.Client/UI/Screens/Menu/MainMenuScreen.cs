using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.MainMenu;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu.Options;
using BetaSharp.Client.UI.Screens.Menu.World;

namespace BetaSharp.Client.UI.Screens.Menu;

public class MainMenuScreen(BetaSharp game) : UIScreen(game)
{
    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Root.AddChild(new Background());

        // --- Logo and Splash ---
        Panel headerPanel = new();
        headerPanel.Style.FlexDirection = FlexDirection.Row;
        headerPanel.Style.JustifyContent = Justify.Center;
        headerPanel.Style.AlignItems = Align.FlexStart;
        headerPanel.Style.MarginBottom = 20;

        MainMenuLogo logo = new();
        headerPanel.AddChild(logo);

        MainMenuSplash splash = new();

        splash.Style.Left = 227;
        splash.Style.Top = 40;
        headerPanel.AddChild(splash);

        Root.AddChild(headerPanel);

        // --- Buttons ---
        TranslationStorage translator = TranslationStorage.Instance;

        Button btnSingleplayer = new() { Text = translator.TranslateKey("menu.singleplayer") };
        btnSingleplayer.OnClick += (e) => Game.displayGuiScreen(new WorldScreen(Game));
        btnSingleplayer.Style.MarginBottom = 4;
        Root.AddChild(btnSingleplayer);

        Button btnMultiplayer = new() { Text = translator.TranslateKey("menu.multiplayer") };
        btnMultiplayer.OnClick += (e) => Game.displayGuiScreen(new MultiplayerScreen(Game));
        btnMultiplayer.Style.MarginBottom = 4;

        if (Game.session == null || Game.session.sessionId == "-")
        {
            btnMultiplayer.Enabled = false;
        }
        Root.AddChild(btnMultiplayer);

        Button btnMods = new() { Text = translator.TranslateKey("menu.mods") };
        btnMods.OnClick += (e) => Game.displayGuiScreen(new TexturePacksScreen(this));
        btnMods.Style.MarginBottom = 4;
        Root.AddChild(btnMods);

        // Options and Quit side-by-side
        Panel footerButtons = new();
        footerButtons.Style.FlexDirection = FlexDirection.Row;
        footerButtons.Style.JustifyContent = Justify.SpaceBetween;
        footerButtons.Style.Width = 200;

        Button btnOptions = new() { Text = translator.TranslateKey("menu.options") };
        btnOptions.Style.Width = 98;
        btnOptions.OnClick += (e) => Game.displayGuiScreen(new OptionsScreen(this, Game.options));

        Button btnQuit = new() { Text = translator.TranslateKey("menu.quit") };
        btnQuit.Style.Width = 98;
        btnQuit.OnClick += (e) => Game.shutdown();

        footerButtons.AddChild(btnOptions);
        if (!Game.hideQuitButton)
        {
            footerButtons.AddChild(btnQuit);
        }
        else
        {
            btnOptions.Style.Width = 200;
        }
        Root.AddChild(footerButtons);

        AddBottomLabels();
    }

    private void AddBottomLabels()
    {
        // Version info
        Label versionLabel = new()
        {
            Text = "BetaSharp " + BetaSharp.Version,
            TextColor = Guis.Color.White
        };
        versionLabel.Style.Position = PositionType.Absolute;
        versionLabel.Style.Left = 2;
        versionLabel.Style.Top = 2;
        versionLabel.OnClick += (e) =>
        {
            var ps = new System.Diagnostics.ProcessStartInfo("https://github.com/betasharp-official/betasharp")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            System.Diagnostics.Process.Start(ps);
        };
        versionLabel.OnMouseEnter += (e) => versionLabel.TextColor = Color.HoverYellow;
        versionLabel.OnMouseLeave += (e) => versionLabel.TextColor = Color.White;

        Root.AddChild(versionLabel);

        // Copyright info
        Panel copyrightPanel = new();
        copyrightPanel.Style.Position = PositionType.Absolute;
        copyrightPanel.Style.Bottom = 2;
        copyrightPanel.Style.Right = 2;
        copyrightPanel.Style.AlignItems = Align.FlexEnd;

        copyrightPanel.AddChild(new Label { Text = "Copyright Mojang Studios. Not an official Minecraft product.", TextColor = Color.White });
        copyrightPanel.AddChild(new Label { Text = "Not approved by or associated with Mojang Studios or Microsoft.", TextColor = Color.White });

        Root.AddChild(copyrightPanel);
    }
}
