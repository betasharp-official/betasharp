using BetaSharp.Client.Rendering.Core;
using BetaSharp.Stats;

namespace BetaSharp.Client.Guis;

public class GuiListStatsGeneral : GuiList
{
    readonly GuiStats parentStatsGui;


    public GuiListStatsGeneral(GuiStats parent) : base(parent.MC, parent.EffectiveWidth, parent.EffectiveHeight, 32, parent.EffectiveHeight - 64, 10)
    {
        parentStatsGui = parent;
        SetShowSelectionHighlight(false);
    }

    public override int GetSize()
    {
        return Stats.Stats.GeneralStats.Count;
    }

    protected override void ElementClicked(int var1, bool var2)
    {
    }

    protected override bool IsSelected(int slotIndex)
    {
        return false;
    }

    protected override int GetContentHeight()
    {
        return GetSize() * 10;
    }

    protected override void DrawBackground()
    {
        parentStatsGui.DrawDefaultBackground();
    }

    protected override void DrawSlot(int index, int x, int y, int rowHeight, Tessellator tessellator)
    {
        StatBase stat = Stats.Stats.GeneralStats[index];
        parentStatsGui.FontRenderer.DrawStringWithShadow(stat.StatName, x + 2, y + 1, index % 2 == 0 ? 0xFFFFFFu : 0x909090u);
        string formatted = stat.Format(parentStatsGui.statFileWriter.ReadStat(stat));
        parentStatsGui.FontRenderer.DrawStringWithShadow(formatted, x + 2 + 213 - parentStatsGui.FontRenderer.GetStringWidth(formatted), y + 1, index % 2 == 0 ? 0xFFFFFF : 0x909090u);
    }
}
