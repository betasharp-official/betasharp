using BetaSharp.Client.Guis;
using BetaSharp.Client.Modding;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class ModListItem(Mod value) : ListItem<Mod>(value)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        renderer.DrawText(Value.Name, 5, 5, Color.White);
        renderer.DrawText($"By {Value.Author}", 5, 17, Color.GrayA0);
    }
}
