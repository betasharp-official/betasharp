using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls.Core;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.ListItems;

public class LanguageListItem(string value) : ListItem<string>(value)
{
    public override void Render(UIRenderer renderer)
    {
        base.Render(renderer);

        Style.Height = 20;

        TranslationStorage translationStorage = TranslationStorage.Instance;

        string displayName = value;
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "Unknown"; // Fallback
        }

        renderer.DrawText(displayName, 5, 6, Color.White);
    }
}
