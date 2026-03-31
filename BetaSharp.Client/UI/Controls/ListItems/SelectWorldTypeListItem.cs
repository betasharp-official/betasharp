using BetaSharp.Client.Guis;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class SelectWorldTypeListItem(WorldType type) : ListItem<WorldType>(type)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        if (!string.IsNullOrEmpty(Value.IconPath))
        {
            TextureHandle texture = renderer.TextureManager.GetTextureId(Value.IconPath);
            renderer.DrawTexture(texture, 4, 4, 24, 24);
        }
        else
        {
            renderer.DrawRect(4, 4, 24, 24, Color.BackgroundBlackAlpha);
        }

        renderer.DrawText(Value.DisplayName, 32, 4, Color.White);
        renderer.DrawText(Value.Description, 32, 16, Color.Gray80);
    }
}
