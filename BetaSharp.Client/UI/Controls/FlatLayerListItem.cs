using BetaSharp.Blocks;
using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Items;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds.Gen.Flat;

namespace BetaSharp.Client.UI.Controls;

public class FlatLayerListItem(FlatLayerInfo layer) : ListItem<FlatLayerInfo>(layer)
{
    private static readonly ItemRenderer s_itemRenderer = new();

    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        Block block = Block.Blocks[Value.FillBlock];
        string blockName = block?.translateBlockName() ?? "Unknown";

        // Draw block icon using existing legacy-style rendering but translated to UI coordinates
        renderer.DrawRect(4, 4, 18, 18, Color.BackgroundBlackAlpha);

        if (block != null)
        {
            // We use the raw GL calls here as needed, but UIRenderer already has Push/PopTranslate
            // so we can just draw at 0,0 locally
            int textureId = block.getTexture(1);
            s_itemRenderer.drawItemIntoGui(BetaSharp.Instance.fontRenderer, BetaSharp.Instance.textureManager, Value.FillBlock, Value.FillBlockMeta, textureId, 5, 5);
        }

        renderer.DrawText(blockName, 26, 4, Color.White);
        renderer.DrawText("Height: " + Value.LayerCount, 26, 16, Color.Gray80);
    }
}
