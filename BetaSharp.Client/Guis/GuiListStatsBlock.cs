using BetaSharp.Client.Guis.Comparators;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Stats;

namespace BetaSharp.Client.Guis;

public class GuiListStatsBlock : GuiListStats<StatCrafting, StatCrafting>
{
    public GuiStats ParentStatsGui { get; }


    public GuiListStatsBlock(GuiStats parent) : base(parent)
    {
        ParentStatsGui = parent;
        StatSorter = new SorterStatsBlock(this, parent);

        Stats = BetaSharp.Stats.Stats.BlocksMinedStats
            .OfType<StatCrafting>()
            .Where(stat =>
                parent.statFileWriter.ReadStat(stat) > 0 ||
                (BetaSharp.Stats.Stats.Used[stat.ItemId] is StatCrafting used && parent.statFileWriter.ReadStat(used) > 0) ||
                (BetaSharp.Stats.Stats.Crafted[stat.ItemId] is StatCrafting crafted && parent.statFileWriter.ReadStat(crafted) > 0))
            .ToList();
    }

    protected override void DrawHeader(int x, int y, Tessellator tessellator)
    {
        base.DrawHeader(x, y, tessellator);

        for (int i = 0; i < 3; i++)
        {
            int offsetX = i switch { 0 => 97, 1 => 147, _ => 197 };
            int uvX = i switch { 0 => 18, 1 => 36, _ => 54 };
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
        DrawStatValue(BetaSharp.Stats.Stats.Crafted[id] as StatCrafting, x + 115, y, isBright);
        DrawStatValue(BetaSharp.Stats.Stats.Used[id] as StatCrafting, x + 165, y, isBright);
        DrawStatValue(stat, x + 215, y, isBright);
    }

    protected override string GetKeyForColumn(int column) => column switch
    {
        0 => "stat.crafted",
        1 => "stat.used",
        _ => "stat.mined",
    };
}
