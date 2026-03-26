using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Client.UI.Screens;
using BetaSharp.Items;

namespace BetaSharp.Client.UI.Controls;

public class FlatPresetListItem(FlatPresetsScreen.PresetItem preset) : ListItem<FlatPresetsScreen.PresetItem>(preset)
{
    private static readonly ItemRenderer s_itemRenderer = new();

    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        // Draw icon
        renderer.DrawRect(4, 4, 18, 18, Color.BackgroundBlackAlpha);

        if (Value.IconId < 256)
        {
            Block block = Block.Blocks[Value.IconId];
            if (block != null)
            {
                int textureId = block.getTexture(1);
                s_itemRenderer.drawItemIntoGui(BetaSharp.Instance.fontRenderer, BetaSharp.Instance.textureManager, Value.IconId, Value.IconMeta, textureId, 5, 5);
            }
        }
        else
        {
            Item item = Item.ITEMS[Value.IconId];
            if (item != null)
            {
                int textureId = item.getTextureId(Value.IconMeta);
                s_itemRenderer.drawItemIntoGui(BetaSharp.Instance.fontRenderer, BetaSharp.Instance.textureManager, Value.IconId, Value.IconMeta, textureId, 5, 5);
            }
        }

        renderer.DrawText(Value.Name, 26, 4, Color.White);
    }
}
