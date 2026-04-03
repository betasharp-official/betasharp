using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Items;

namespace BetaSharp.Client.UI.Controls;

public class StatItemRow : UIElement
{
    private const float TextY = 8;
    private const float IconSize = 18;
    private const float IconX = 4;
    private const float IconY = 3;

    public int ItemId { get; set; }
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
    public string Value3 { get; set; } = string.Empty;
    public bool IsAlternate { get; set; }

    public StatItemRow(int itemId, string v1, string v2, string v3, bool alternate)
    {
        ItemId = itemId;
        Value1 = v1;
        Value2 = v2;
        Value3 = v3;
        IsAlternate = alternate;

        Style.Height = 24;
        Style.Width = null; // Fill parent
    }

    public override void Render(UIRenderer renderer)
    {
        DrawBackground(renderer);

        renderer.DrawRect(IconX, IconY, IconSize, IconSize, Color.BackgroundBlackAlpha);
        renderer.DrawItem(new ItemStack(ItemId, 1, 0), IconX + 1, IconY + 1);

        // don't draw unlocalized names for now
        string? name = Item.ITEMS[ItemId]?.getStatName();
        if (name != null && !name.Contains('.'))
        {
            renderer.DrawText(name, 28, TextY, Color.White);
        }

        float rightOffset = ComputedWidth - 10;
        renderer.DrawText(Value3, rightOffset - 30, TextY, Color.White);
        renderer.DrawText(Value2, rightOffset - 80, TextY, Color.White);
        renderer.DrawText(Value1, rightOffset - 130, TextY, Color.White);

        base.Render(renderer);
    }

    private void DrawBackground(UIRenderer renderer)
    {
        if (IsAlternate)
        {
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, new Color(Color.White, 10));
        }

        if (IsHovered)
        {
            renderer.DrawRect(0, 0, ComputedWidth, ComputedHeight, Color.WhiteAlpha20);
        }
    }
}
