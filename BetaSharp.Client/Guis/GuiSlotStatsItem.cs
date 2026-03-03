using BetaSharp.Client.Guis.Comparators;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Stats;

namespace BetaSharp.Client.Guis;

public class GuiSlotStatsItem : GuiSlotStats<StatCrafting, StatCrafting>
{
    public GuiStats ParentStatsGui { get; }


    public GuiSlotStatsItem(GuiStats parent) : base(parent)
    {
        ParentStatsGui = parent;
        StatSorter = new SorterStatsItem(this, parent);

        Stats = global::BetaSharp.Stats.Stats.ItemStats
            .OfType<StatCrafting>()
            .Where(stat =>
                parent.statFileWriter.GetStatValue(stat) > 0 ||
                (global::BetaSharp.Stats.Stats.Broken[stat.ItemId] is StatCrafting broken && parent.statFileWriter.GetStatValue(broken) > 0) ||
                (global::BetaSharp.Stats.Stats.Crafted[stat.ItemId] is StatCrafting crafted && parent.statFileWriter.GetStatValue(crafted) > 0))
            .ToList();
    }

    protected override void DrawHeader(int x, int y, Tessellator tessellator)
    {
        base.DrawHeader(x, y, tessellator);

        for (int i = 0; i < 3; i++)
        {
            int offsetX = i switch { 0 => 97, 1 => 147, _ => 197 };
            int uvX = i switch { 0 => 72, 1 => 18, _ => 36 };
            int hoverOffset = HoveredColumn == i ? 1 : 0;

            ParentStatsGui.drawTranslucentRect(x + offsetX + hoverOffset, y + 1 + hoverOffset, uvX, 18);
        }
    }

    protected override void DrawSlot(int index, int x, int y, int rowHeight, Tessellator tessellator)
    {
        StatCrafting stat = GetStat(index);
        int id = stat.ItemId;

        ParentStatsGui.drawItemSlot(x + 40, y, id);

        bool isBright = index % 2 == 0;
        DrawStatValue(global::BetaSharp.Stats.Stats.Broken[id] as StatCrafting, x + 115, y, isBright);
        DrawStatValue(global::BetaSharp.Stats.Stats.Crafted[id] as StatCrafting, x + 165, y, isBright);
        DrawStatValue(stat, x + 215, y, isBright);
    }

    protected override string GetKeyForColumn(int column) => column switch
    {
        1 => "stat.crafted",
        2 => "stat.used",
        _ => "stat.depleted"
    };
}
