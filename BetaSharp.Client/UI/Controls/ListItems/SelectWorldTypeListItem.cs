using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds;

namespace BetaSharp.Client.UI.Controls.ListItems;

/// <summary>
/// List item for a single world type (e.g. flat, normal)
/// </summary>
public class SelectWorldTypeListItem(WorldType type) : ListItem<WorldType>(type)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        // draw icon
        if (!string.IsNullOrEmpty(Value.IconPath))
        {
            TextureHandle texture = renderer.TextureManager.GetTextureId(Value.IconPath);
            renderer.DrawTexture(texture, 4, 4, 24, 24);
        }
        else
        {
            renderer.DrawRect(4, 4, 24, 24, Color.BackgroundBlackAlpha);
        }

        // draw info
        renderer.DrawText(Value.DisplayName, 32, 4, Color.White);
        renderer.DrawText(Value.Description, 32, 16, Color.Gray80);
    }
}
