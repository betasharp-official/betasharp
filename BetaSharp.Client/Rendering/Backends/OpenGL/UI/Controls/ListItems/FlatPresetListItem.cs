using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Client.UI.Screens.Menu.World;
using BetaSharp.Items;

namespace BetaSharp.Client.UI.Controls.ListItems;

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
                int textureId = block.GetTexture(Side.Up);
                renderer.DrawItemIntoGui(s_itemRenderer, Value.IconId, Value.IconMeta, textureId, 5, 5);
            }
        }
        else
        {
            Item item = Item.ITEMS[Value.IconId];
            if (item != null)
            {
                int textureId = item.getTextureId(Value.IconMeta);
                renderer.DrawItemIntoGui(s_itemRenderer, Value.IconId, Value.IconMeta, textureId, 5, 5);
            }
        }

        renderer.DrawText(Value.Name, 26, 4, Color.White);
    }
}
