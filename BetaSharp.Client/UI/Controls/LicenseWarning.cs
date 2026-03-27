using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls;

public class LicenseWarning(BetaSharp game) : UIElement
{
    private readonly BetaSharp _game = game;

    public override void Render(UIRenderer renderer)
    {
        if (!Visible) return;

        int y = 0;
        if (_game.IsMainMenuOpen) y += 9;

        renderer.DrawText("Unlicensed Copy :(", 0, y, Color.White, shadow: true);
        renderer.DrawText("(Or logged in from another location)", 0, y + 9, Color.White, shadow: true);
        renderer.DrawText("Purchase Minecraft at minecraft.net", 0, y + 18, Color.White, shadow: true);
    }
}
