using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Worlds.Storage;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class WorldListItem(WorldSaveInfo value) : ListItem<WorldSaveInfo>(value)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        string displayName = Value.DisplayName;
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "World"; // Fallback
        }

        renderer.DrawText(displayName, 5, 5, Color.White);

        string dateFormatPattern = "MMM d, yyyy HH:mm";
        DateTime lastPlayed = DateTimeOffset.FromUnixTimeMilliseconds(Value.LastPlayed).ToLocalTime().DateTime;

        string secondary = $"{Value.FileName} ({lastPlayed.ToString(dateFormatPattern)}, {Value.Size / 1024L / 1024.0F:F2} MB)";

        if (Value.IsUnsupported)
        {
            secondary = "Unsupported Format! " + secondary;
        }

        renderer.DrawText(secondary, 5, 17, Color.GrayA0);
    }
}
