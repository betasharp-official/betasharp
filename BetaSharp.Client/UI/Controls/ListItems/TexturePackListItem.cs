using BetaSharp.Client.Guis;
using BetaSharp.Client.Resource.Pack;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.ListItems;

/// <summary>
/// List item for a texture pack, showing the thumbnail, name, and description.
/// </summary>
/// <param name="value"></param>
public class TexturePackListItem(TexturePack value) : ListItem<TexturePack>(value)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        // draw thumnail
        Value.BindThumbnailTexture(renderer.TextureManager);
        renderer.DrawBoundTexture(4, 4, 24, 24);

        // get file name and draw
        string? fileName = Value.TexturePackFileName;
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "Unknown Pack";
        }

        renderer.DrawText(fileName, 32, 3, Color.White);

        // descriptions
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
