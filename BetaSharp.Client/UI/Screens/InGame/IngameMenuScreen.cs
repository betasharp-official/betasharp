using System.Diagnostics;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Screens.Menu.Options;
using BetaSharp.Stats;

namespace BetaSharp.Client.UI.Screens.InGame;

public class IngameMenuScreen(
    UIContext context,
    StatFileWriter statFileWriter,
    Action onResume,
    string quitButtonText,
    Action quit,
    Func<bool> isSavingComplete,
    TexturePacks texturePacks) : UIScreen(context)
{
    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.FlexStart;

        Root.AddChild(new Background(BackgroundType.World));

        Label title = new() { Text = "Game menu", TextColor = Color.White };
        title.Style.MarginTop = 20;
        //title.Style.MarginBottom = 8;
        Root.AddChild(title);
        AddTitleSpacer();

        TranslationStorage translator = TranslationStorage.Instance;

        Button btnBack = CreateButton();
        btnBack.Text = "Back to Game";
        btnBack.OnClick += (e) =>
        {
            Context.Navigator.Navigate(null);
            onResume();
        };
        btnBack.Style.MarginBottom = 4;
        Root.AddChild(btnBack);

        // --- Stats & Achievements Row ---
        Panel rowStats = new();
        rowStats.Style.FlexDirection = FlexDirection.Row;
        rowStats.Style.JustifyContent = Justify.SpaceBetween;
        rowStats.Style.Width = 200;
        rowStats.Style.MarginBottom = 4;

        Button btnAchievements = CreateButton();
        btnAchievements.Text = StatCollector.TranslateToLocal("gui.achievements");
        btnAchievements.Style.Width = 98;
        btnAchievements.OnClick += (e) => Context.Navigator.Navigate(new AchievementsScreen(Context, this, statFileWriter));

        Button btnStats = CreateButton();
        btnStats.Text = StatCollector.TranslateToLocal("gui.stats");
        btnStats.Style.Width = 98;
        btnStats.OnClick += (e) => Context.Navigator.Navigate(new StatsScreen(Context, this, statFileWriter));

        rowStats.AddChild(btnAchievements);
        rowStats.AddChild(btnStats);
        Root.AddChild(rowStats);

        // --- Send Feedback and Report Bugs Row ---
        Panel feedbackRow = new();
        feedbackRow.Style.FlexDirection = FlexDirection.Row;
        feedbackRow.Style.JustifyContent = Justify.SpaceBetween;
        feedbackRow.Style.Width = 200;
        feedbackRow.Style.MarginBottom = 4;

        Button btnFeedback = CreateButton();
        btnFeedback.Text = "Send Feedback";
        btnFeedback.Style.Width = 98;
        btnFeedback.OnClick += (e) => OpenLink("https://github.com/betasharp-official/betasharp/issues/new");

        Button btnReport = CreateButton();
        btnReport.Text = "Report Bugs";
        btnReport.Style.Width = 98;
        btnReport.OnClick += (e) => OpenLink("https://github.com/betasharp-official/betasharp/issues/new?template=bug_report.yml");

        feedbackRow.AddChild(btnFeedback);
        feedbackRow.AddChild(btnReport);
        Root.AddChild(feedbackRow);

        Button btnOptions = CreateButton();
        btnOptions.Text = translator.TranslateKey("menu.options");
        btnOptions.OnClick += (e) => Context.Navigator.Navigate(new OptionsScreen(Context, this, texturePacks));
        btnOptions.Style.MarginBottom = 4;
        Root.AddChild(btnOptions);
   
        Button btnQuit = CreateButton();
        btnQuit.Text = quitButtonText;
        btnQuit.OnClick += (e) =>
        {
            statFileWriter.ReadStat(global::BetaSharp.Stats.Stats.LeaveGameStat, 1);
            quit();
            Context.Navigator.Navigate(null);
        };
        Root.AddChild(btnQuit);

        SavingIndicator savingIndicator = new(isSavingComplete);
        savingIndicator.Style.Position = PositionType.Absolute;
        savingIndicator.Style.Left = 4;
        savingIndicator.Style.Bottom = 4;
        Root.AddChild(savingIndicator);
    }

    private void OpenLink(string url) {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
