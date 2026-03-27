using BetaSharp.Client.Guis;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class TexturePackListItem(TexturePack value) : ListItem<TexturePack>(value)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        Value.BindThumbnailTexture(BetaSharp.Instance);
        renderer.DrawBoundTexture(4, 4, 24, 24);

        string? fileName = Value.TexturePackFileName;
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "Unknown Pack";
        }

        renderer.DrawText(fileName, 32, 3, Color.White);

        if (!string.IsNullOrEmpty(Value.FirstDescriptionLine))
        {
            renderer.DrawText(Value.FirstDescriptionLine, 32, 12, Color.GrayA0);
        }

        if (!string.IsNullOrEmpty(Value.SecondDescriptionLine))
        {
            renderer.DrawText(Value.SecondDescriptionLine, 32, 21, Color.GrayA0);
        }
    }
}
