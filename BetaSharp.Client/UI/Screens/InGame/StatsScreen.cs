using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Stats;

namespace BetaSharp.Client.UI.Screens.InGame;

public class StatsScreen(UIContext context, UIScreen? parent, StatFileWriter stats) : UIScreen(context)
{
    private enum Tab { General, Blocks, Items }
    private Tab _currentTab = Tab.General;
    private readonly StatFileWriter _stats = stats;
    private Panel? _contentPanel;
    private Button? _btnGeneral, _btnBlocks, _btnItems;

    protected override void Init()
    {
        Root.Style.AlignItems = Align.Center;
        Root.Style.JustifyContent = Justify.FlexStart;

        Root.AddChild(new Background(BackgroundType.World));

        Label title = new() { Text = "Statistics", TextColor = Color.White };
        title.Style.MarginTop = 20;
        title.Style.MarginBottom = 8;
        Root.AddChild(title);

        AddTitleSpacer();

        // Tab bar
        Panel tabBar = new();
        tabBar.Style.FlexDirection = FlexDirection.Row;
        tabBar.Style.Height = 30;
        tabBar.Style.MarginBottom = 6;

        _btnGeneral = CreateTabButton("General", Tab.General);
        _btnBlocks = CreateTabButton("Blocks", Tab.Blocks);
        _btnItems = CreateTabButton("Items", Tab.Items);

        tabBar.AddChild(_btnGeneral);
        tabBar.AddChild(_btnBlocks);
        tabBar.AddChild(_btnItems);
        Root.AddChild(tabBar);

        // Content area
        _contentPanel = new();
        _contentPanel.Style.Width = 360;
        _contentPanel.Style.FlexGrow = 1;
        _contentPanel.Style.MaxHeight = 164; // 200 - tabBar(36) = aligns Done with options
        _contentPanel.Style.BackgroundColor = new Color(0, 0, 0, 160);
        _contentPanel.Style.SetPadding(5);
        Root.AddChild(_contentPanel);

        // Done button
        Button btnDone = CreateButton();
        btnDone.Text = "Done";
        btnDone.Style.MarginTop = 10;
        btnDone.Style.MarginBottom = 20;
        btnDone.Style.FlexShrink = 0; // Prevent squeezing
        btnDone.OnClick += (_) => Context.Navigator.Navigate(parent);
        Root.AddChild(btnDone);

        UpdateTab(Tab.General);
    }

    private Button CreateTabButton(string text, Tab tab)
    {
        Button btn = CreateButton();
        btn.Text = text;
        btn.Style.Width = 100;
        btn.Style.MarginLeft = 4;
        btn.Style.MarginRight = 4;
        btn.OnClick += (_) => UpdateTab(tab);
        return btn;
    }

    private void UpdateTab(Tab tab)
    {
        _currentTab = tab;
        _contentPanel!.Children.Clear();

        // disabled = currently active
        _btnGeneral!.Enabled = _currentTab != Tab.General;
        _btnBlocks!.Enabled = _currentTab != Tab.Blocks;
        _btnItems!.Enabled = _currentTab != Tab.Items;

        ScrollView scrollView = new();
        scrollView.Style.FlexGrow = 1.0f;
        _contentPanel!.AddChild(scrollView);

        Panel list = new();
        list.Style.FlexDirection = FlexDirection.Column;
        scrollView.AddContent(list);

        switch (tab)
        {
            case Tab.General:
                PopulateGeneralStats(list);
                break;
            case Tab.Blocks:
                PopulateBlocksStats(list);
                break;
            case Tab.Items:
                PopulateItemsStats(list);
                break;
        }

        Root.OnLayoutApplied(new() { MeasureString = (s) => Context.TextRenderer.GetStringWidth(s) }); // Update layout for the new content
    }

    private void PopulateGeneralStats(Panel list)
    {
        List<StatBase> stats = Stats.Stats.GeneralStats;
        for (int i = 0; i < stats.Count; i++)
        {
            StatBase stat = stats[i];
            int value = _stats.GetStatValue(stat);
            string formatted = stat.Format(value);

            Panel row = new();
            row.Style.FlexDirection = FlexDirection.Row;
            row.Style.JustifyContent = Justify.SpaceBetween;
            row.Style.AlignItems = Align.Center; // Vertical centering
            row.Style.PaddingLeft = 10;
            row.Style.PaddingRight = 10;
            row.Style.Height = 22;
            if (i % 2 == 1) row.Style.BackgroundColor = new Color(255, 255, 255, 10);

            row.AddChild(new Label { Text = stat.StatName, TextColor = Color.White });
            row.AddChild(new Label { Text = formatted, TextColor = Color.White });
            list.AddChild(row);
        }
    }

    private void PopulateBlocksStats(Panel list)
    {
        AddHeaderRow(list, "Mined", "Crafted", "Used");

        var blockStats = Stats.Stats.BlocksMinedStats
            .OfType<StatCrafting>()
            .Where(stat =>
                 _stats.GetStatValue(stat) > 0 ||
                (Stats.Stats.Used[stat.ItemId] is StatCrafting used && _stats.GetStatValue(used) > 0) ||
                (Stats.Stats.Crafted[stat.ItemId] is StatCrafting crafted && _stats.GetStatValue(crafted) > 0))
            .ToList();

        for (int i = 0; i < blockStats.Count; i++)
        {
            StatCrafting minedStat = blockStats[i];
            int id = minedStat.ItemId;

            string v1 = minedStat.Format(_stats.GetStatValue(minedStat));
            string v2 = Stats.Stats.Crafted[id] is StatCrafting craftedStat ? craftedStat.Format(_stats.GetStatValue(craftedStat)) : "0";
            string v3 = Stats.Stats.Used[id] is StatCrafting usedStat ? usedStat.Format(_stats.GetStatValue(usedStat)) : "0";

            list.AddChild(new StatItemRow(id, v1, v2, v3, i % 2 == 1));
        }
    }

    private void PopulateItemsStats(Panel list)
    {
        AddHeaderRow(list, "Broken", "Crafted", "Used");

        var itemStats = Stats.Stats.ItemStats
            .OfType<StatCrafting>()
            .Where(stat =>
                _stats.GetStatValue(stat) > 0 ||
                (Stats.Stats.Broken[stat.ItemId] is StatCrafting broken && _stats.GetStatValue(broken) > 0) ||
                (Stats.Stats.Crafted[stat.ItemId] is StatCrafting crafted && _stats.GetStatValue(crafted) > 0))
            .ToList();

        for (int i = 0; i < itemStats.Count; i++)
        {
            StatCrafting brokenStat = itemStats[i];
            int id = brokenStat.ItemId;

            string v1 = brokenStat.Format(_stats.GetStatValue(brokenStat));
            string v2 = Stats.Stats.Crafted[id] is StatCrafting craftedStat ? craftedStat.Format(_stats.GetStatValue(craftedStat)) : "0";
            string v3 = Stats.Stats.Used[id] is StatCrafting usedStat ? usedStat.Format(_stats.GetStatValue(usedStat)) : "0";

            list.AddChild(new StatItemRow(id, v1, v2, v3, i % 2 == 1));
        }
    }

    private static void AddHeaderRow(Panel list, string h1, string h2, string h3)
    {
        Panel row = new();
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.AlignItems = Align.Center; // Vertical centering
        row.Style.Height = 20;
        row.Style.PaddingLeft = 10;
        row.Style.PaddingRight = 10;
        row.Style.BackgroundColor = new Color(255, 255, 255, 30);
        row.Style.MarginBottom = 2;
        row.Style.AlignItems = Align.Center; // Align headers

        row.AddChild(new Label { Text = "Item", TextColor = Color.GrayA0 });

        // Custom panel to align headers to the right
        Panel spacer = new();
        spacer.Style.FlexGrow = 1;
        row.AddChild(spacer);

        row.AddChild(CreateHeaderLabel(h1));
        row.AddChild(CreateHeaderLabel(h2));
        row.AddChild(CreateHeaderLabel(h3));

        list.AddChild(row);
    }

    private static Label CreateHeaderLabel(string text)
    {
        Label lbl = new() { Text = text, TextColor = Color.GrayA0 };
        lbl.Style.Width = 50;
        return lbl;
    }
}
