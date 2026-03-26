using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds;

namespace BetaSharp.Client.UI.Controls;

public class SelectWorldTypeListItem(WorldType type) : ListItem<WorldType>(type)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        // Icon placeholder or specific icon
        renderer.DrawRect(4, 4, 24, 24, Color.BackgroundBlackAlpha);
        // TODO: Render actual icon if available in modernized way

        renderer.DrawText(Value.DisplayName, 32, 4, Color.White);
        renderer.DrawText(Value.Description, 32, 16, Color.Gray80);
    }
}
