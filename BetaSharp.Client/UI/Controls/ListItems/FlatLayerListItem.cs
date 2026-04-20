using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds.Gen.Flat;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class FlatLayerListItem(FlatLayerInfo layer) : ListItem<FlatLayerInfo>(layer)
{
    private static readonly ItemRenderer s_itemRenderer = new();

    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        Block block = Block.Blocks[Value.FillBlock];
        string blockName = block?.TranslateBlockName() ?? "Unknown";

        renderer.DrawRect(4, 4, 18, 18, Color.BackgroundBlackAlpha);

        if (block != null)
        {
            int textureId = block.GetTexture(Side.Up);
            renderer.DrawItemIntoGui(s_itemRenderer, Value.FillBlock, Value.FillBlockMeta, textureId, 5, 5);
        }

        renderer.DrawText(blockName, 26, 4, Color.White);
        renderer.DrawText("Height: " + Value.LayerCount, 26, 16, Color.Gray80);
    }
}
