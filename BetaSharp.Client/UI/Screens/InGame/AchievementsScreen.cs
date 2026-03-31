using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Achievement;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Stats;

namespace BetaSharp.Client.UI.Screens.InGame;

public class AchievementsScreen(
    UIContext context,
    UIScreen? parent,
    StatFileWriter stats) : UIScreen(context)
{
    public override bool PausesGame => true;

    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.FlexStart;

        Root.AddChild(new Background(BackgroundType.World));

        // Title
        Label title = new() { Text = "Achievements", TextColor = Color.White };
        title.Style.MarginTop = 20;
        title.Style.MarginBottom = 8;
        Root.AddChild(title);

        AddTitleSpacer();

        // Stats summary
        int total = global::BetaSharp.Achievements.AllAchievements.Count;
        int unlockedCount = global::BetaSharp.Achievements.AllAchievements.Count(a => stats.HasAchievementUnlocked(a));
        float progress = (float)unlockedCount / total;

        Label progressLabel = new() { Text = $"{unlockedCount} / {total} ({progress:P0})", TextColor = Color.GrayA0 };
        progressLabel.Style.MarginBottom = 6;
        Root.AddChild(progressLabel);

        // Progress Bar (Slim)
        Panel progressBarBg = new();
        progressBarBg.Style.Height = 4;
        progressBarBg.Style.Width = 350;
        progressBarBg.Style.BackgroundColor = Color.Black;
        progressBarBg.Style.MarginBottom = 10;
        Root.AddChild(progressBarBg);

        Panel progressBarFill = new();
        progressBarFill.Style.Height = 4;
        progressBarFill.Style.Width = progress * 350;
        progressBarFill.Style.BackgroundColor = Color.AchievementTakenBlue;
        progressBarBg.AddChild(progressBarFill);

        // Main Content Area (The "Dashboard")
        Panel contentPanel = new();
        contentPanel.Style.Width = 380;
        contentPanel.Style.FlexGrow = 1;
        contentPanel.Style.MaxHeight = 172; // 200 - progressLabel(14) - progressBar(14) = aligns Done with options
        contentPanel.Style.BackgroundColor = new Color(0, 0, 0, 160);
        contentPanel.Style.SetPadding(4);
        Root.AddChild(contentPanel);

        // Scrollable area
        ScrollView scrollView = new();
        scrollView.Style.FlexGrow = 1;
        contentPanel.AddChild(scrollView);

        Panel cardList = new();
        cardList.Style.FlexDirection = FlexDirection.Column;
        cardList.Style.Width = null;
        scrollView.AddContent(cardList);

        PopulateAchievementList(cardList);

        Button btnDone = CreateButton();
        btnDone.Text = "Done";
        btnDone.Style.MarginTop = 10;
        btnDone.Style.MarginBottom = 20;
        btnDone.Style.FlexShrink = 0;
        btnDone.OnClick += (_) => Context.Navigator.Navigate(parent);
        Root.AddChild(btnDone);
    }

    private void PopulateAchievementList(Panel list)
    {
        List<Achievement> all = global::BetaSharp.Achievements.AllAchievements;

        var roots = all.Where(a => a.parent == null).ToList();
        foreach (Achievement? root in roots)
        {
            AddAchievementRecursively(list, root, 0);
        }
    }

    private void AddAchievementRecursively(Panel list, Achievement ach, int indent)
    {
        AchievementCard card = new(ach, stats);
        card.Style.MarginLeft = indent;
        list.AddChild(card);

        var children = global::BetaSharp.Achievements.AllAchievements.Where(a => a.parent == ach).ToList();
        foreach (Achievement? child in children)
        {
            AddAchievementRecursively(list, child, indent + 16);
        }
    }
}
