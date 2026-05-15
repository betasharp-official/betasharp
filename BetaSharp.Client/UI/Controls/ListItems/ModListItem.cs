using BetaSharp.Client.Guis;
using BetaSharp.Client.Modding;
using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class ModListItem(Mod value) : ListItem<Mod>(value)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);
        Style.Height = 40;

        renderer.DrawTexture(renderer.TextureManager.GetTextureId(Value.Icon), 4, 4, 32, 32);
        renderer.DrawText(Value.Name, 40, 5, Color.White);
        renderer.DrawText(TranslationStorage.Instance.TranslateKeyFormat("mods.by", Value.Author), 40, 17, Color.GrayA0);
    }
}
