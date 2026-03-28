using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu;
using BetaSharp.Client.UI.Screens.Menu.Options;
using BetaSharp.Stats;

namespace BetaSharp.Client.UI.Screens.InGame;

public class IngameMenuScreen(BetaSharp game) : UIScreen(game)
{
    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.Center;

        Root.AddChild(new Background(BackgroundType.World));

        Label title = new() { Text = "Game menu", TextColor = Color.White };
        title.Style.MarginBottom = 16;
        Root.AddChild(title);

        TranslationStorage translator = TranslationStorage.Instance;

        Button btnBack = new() { Text = "Back to Game" };
        btnBack.OnClick += (e) =>
        {
            Game.displayGuiScreen(null);
            Game.setIngameFocus();
        };
        btnBack.Style.MarginBottom = 4;
        Root.AddChild(btnBack);

        // --- Stats & Achievements Row ---
        Panel rowStats = new();
        rowStats.Style.FlexDirection = FlexDirection.Row;
        rowStats.Style.JustifyContent = Justify.SpaceBetween;
        rowStats.Style.Width = 200;
        rowStats.Style.MarginBottom = 4;

        Button btnAchievements = new() { Text = StatCollector.TranslateToLocal("gui.achievements") };
        btnAchievements.Style.Width = 98;
        btnAchievements.OnClick += (e) => Game.displayGuiScreen(new AchievementsScreen(this, Game.statFileWriter));

        Button btnStats = new() { Text = StatCollector.TranslateToLocal("gui.stats") };
        btnStats.Style.Width = 98;
        btnStats.OnClick += (e) => Game.displayGuiScreen(new StatsScreen(this, Game.statFileWriter));

        rowStats.AddChild(btnAchievements);
        rowStats.AddChild(btnStats);
        Root.AddChild(rowStats);

        Button btnOptions = new() { Text = translator.TranslateKey("menu.options") };
        btnOptions.OnClick += (e) => Game.displayGuiScreen(new OptionsScreen(this, Game.options));
        btnOptions.Style.MarginBottom = 4;
        Root.AddChild(btnOptions);

        string quitText = (Game.isMultiplayerWorld() && Game.internalServer == null) ? "Disconnect" : "Save and quit to title";
        Button btnQuit = new() { Text = quitText };
        btnQuit.OnClick += (e) =>
        {
            Game.statFileWriter.ReadStat(Stats.Stats.LeaveGameStat, 1);
            if (Game.isMultiplayerWorld())
            {
                Game.world.Disconnect();
            }

            Game.stopInternalServer();
            Game.changeWorld(null!);
            Game.options.ShowDebugInfo = false;
            Game.displayGuiScreen(new MainMenuScreen(Game));
        };
        Root.AddChild(btnQuit);

        SavingIndicator savingIndicator = new();
        savingIndicator.Style.Position = PositionType.Absolute;
        savingIndicator.Style.Left = 8;
        savingIndicator.Style.Bottom = 8;
        Root.AddChild(savingIndicator);
    }
}
