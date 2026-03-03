using BetaSharp.Client.Input;
using BetaSharp.Client.Rendering.Core;
using BetaSharp.Items;
using BetaSharp.Stats;

namespace BetaSharp.Client.Guis;

public abstract class GuiSlotStats<T, K>(GuiStats statsGui) : GuiSlot(statsGui.Game, statsGui.Width, statsGui.Height, 32, statsGui.Height - 64, 20)
    where K : class, T
{
    public int ActiveStatType { get; set; } = -1;
    public int SortOrder { get; set; }

    protected int HoveredColumn { get; set; } = -1;
    protected List<K> Stats { get; set; } = [];
    protected IComparer<T> StatSorter { get; set; } = null!;

    protected override void ElementClicked(int slotIndex, bool doubleClick) { }

    protected override bool IsSelected(int slotIndex) => false;

    protected override void DrawBackground() => statsGui.DrawDefaultBackground();

    protected override void DrawHeader(int x, int y, Tessellator tess)
    {
        if (!Mouse.isButtonDown(0))
            HoveredColumn = -1;

        for (int i = 0; i < 3; i++)
        {
            int offsetX = i switch { 0 => 97, 1 => 147, _ => 197 };
            int offsetY = HoveredColumn == i ? 0 : 18;
            statsGui.drawTranslucentRect(x + offsetX, y + 1, 0, offsetY);
        }

        if (ActiveStatType != -1)
        {
            int rectX = ActiveStatType switch { 1 => 129, 2 => 179, _ => 79 };
            int rectY = SortOrder == 1 ? 36 : 18;
            statsGui.drawTranslucentRect(x + rectX, y + 1, rectY, 0);
        }
    }

    protected override void HeaderClicked(int mouseX, int mouseY)
    {
        HoveredColumn = -1;
        if (mouseX is >= 79 and < 115) HoveredColumn = 0;
        else if (mouseX is >= 129 and < 165) HoveredColumn = 1;
        else if (mouseX is >= 179 and < 215) HoveredColumn = 2;

        if (HoveredColumn >= 0)
        {
            SortByColumn(HoveredColumn);
            statsGui.Game.sndManager.PlaySoundFX("random.click", 1.0F, 1.0F);
        }
    }

    public override int GetSize() => Stats.Count;

    protected StatCrafting GetStat(int i) => (Stats[i] as StatCrafting)!;

    protected abstract string GetKeyForColumn(int column);

    protected void DrawStatValue(StatCrafting? stat, int x, int y, bool useBrightColor)
    {
        string text = stat is not null ? stat.Format(statsGui.statFileWriter.GetStatValue(stat)) : "-";
        statsGui.FontRenderer.DrawStringWithShadow(text, x - statsGui.FontRenderer.GetStringWidth(text), y + 5, useBrightColor ? Color.White : Color.Gray90);
    }

    protected override void PostDrawScreen(int mouseX, int mouseY)
    {
        if (mouseY >= _top && mouseY <= _bottom)
        {
            int slotIndex = GetSlotAt(mouseX, mouseY);
            int centerX = statsGui.Width / 2 - 108;

            if (slotIndex >= 0)
            {
                if (mouseX < centerX + 40 || mouseX > centerX + 60) return;
                DrawItemTooltip(GetStat(slotIndex), mouseX, mouseY);
            }
            else
            {
                int relX = mouseX - centerX;
                string key = relX switch
                {
                    var m when m is >= 97 and <= 115 => GetKeyForColumn(0),
                    var m when m is >= 147 and <= 165 => GetKeyForColumn(1),
                    var m when m is >= 197 and <= 215 => GetKeyForColumn(2),
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(key))
                {
                    string translated = TranslationStorage.Instance.TranslateKey(key).Trim();
                    if (translated.Length > 0)
                    {
                        int textX = mouseX + 12;
                        int textY = mouseY - 12;
                        int textWidth = statsGui.FontRenderer.GetStringWidth(translated);
                        statsGui.drawTranslucentRect(textX - 3, textY - 3, textX + textWidth + 3, textY + 8 + 3);
                        statsGui.FontRenderer.DrawStringWithShadow(translated, textX, textY, Color.White);
                    }
                }
            }
        }
    }

    protected void DrawItemTooltip(StatCrafting? stat, int x, int y)
    {
        if (stat is not null)
        {
            Item item = Item.ITEMS[stat.ItemId];
            string translated = TranslationStorage.Instance.TranslateNamedKey(item.getItemName()).Trim();
            if (translated.Length > 0)
            {
                int textX = x + 12;
                int textY = y - 12;
                int textWidth = statsGui.FontRenderer.GetStringWidth(translated);
                statsGui.drawTranslucentRect(textX - 3, textY - 3, textX + textWidth + 3, textY + 8 + 3);
                statsGui.FontRenderer.DrawStringWithShadow(translated, textX, textY, Color.White);
            }
        }
    }

    protected void SortByColumn(int columnIndex)
    {
        if (columnIndex != ActiveStatType)
        {
            ActiveStatType = columnIndex;
            SortOrder = -1;
        }
        else if (SortOrder == -1)
        {
            SortOrder = 1;
        }
        else
        {
            ActiveStatType = -1;
            SortOrder = 0;
        }

        Stats.Sort((x, y) => StatSorter.Compare(x, y));
    }
}
