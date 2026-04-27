using BetaSharp.Client.Guis;
using BetaSharp.Client.Network;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Controls.MainMenu;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu.Options;
using BetaSharp.Client.UI.Screens.Menu.World;
namespace BetaSharp.Client.UI.Screens.Menu;

public class MainMenuScreen(
    UIContext context,
    Session? session,
    bool hideQuitButton,
    ISingleplayerHost singleplayerHost,
    ClientNetworkContext networkContext,
    TexturePacks texturePackList,
    Action shutdown) : UIScreen(context)
{
    private const float LogoTopPadding = 30f;

    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.FlexStart;
        Root.Style.PaddingTop = LogoTopPadding;

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

        AddTitleSpacer(LogoTopPadding + 64f, 48f);

        // --- Buttons ---
        TranslationStorage translator = TranslationStorage.Instance;

        Button btnSingleplayer = CreateButton();
        btnSingleplayer.Text = translator.TranslateKey("menu.singleplayer");
        btnSingleplayer.OnClick += (e) => Context.Navigator.Navigate(new WorldScreen(Context, singleplayerHost));
        btnSingleplayer.Style.MarginBottom = 4;
        Root.AddChild(btnSingleplayer);

        Button btnMultiplayer = CreateButton();
        btnMultiplayer.Text = translator.TranslateKey("menu.multiplayer");
        btnMultiplayer.OnClick += (e) => Context.Navigator.Navigate(new MultiplayerScreen(Context, networkContext));
        btnMultiplayer.Style.MarginBottom = 4;

        if (session == null || session.sessionId == "-")
        {
            btnMultiplayer.Enabled = false;
        }
        Root.AddChild(btnMultiplayer);

        // Options and Quit side-by-side
        Panel footerButtons = new();
        footerButtons.Style.FlexDirection = FlexDirection.Row;
        footerButtons.Style.JustifyContent = Justify.SpaceBetween;
        footerButtons.Style.Width = 200;
        footerButtons.Style.MarginTop = 16;

        Button btnOptions = CreateButton();
        btnOptions.Text = translator.TranslateKey("menu.options");
        btnOptions.Style.Width = 98;
        btnOptions.OnClick += (e) => Context.Navigator.Navigate(new OptionsScreen(Context, this, texturePackList));

        Button btnQuit = CreateButton();
        btnQuit.Text = translator.TranslateKey("menu.quit");
        btnQuit.Style.Width = 98;
        btnQuit.OnClick += (e) => shutdown();

        footerButtons.AddChild(btnOptions);
        if (!hideQuitButton)
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

    public override void KeyTyped(int key, char character) { }
}
