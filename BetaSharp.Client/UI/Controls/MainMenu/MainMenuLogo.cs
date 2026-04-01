using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.MainMenu;

public class MainMenuLogo : UIElement
{
    private const short LogoWidth = 274;
    private const byte LogoHeight = 44;

    public MainMenuLogo()
    {
        Style.Width = LogoWidth;
        Style.Height = LogoHeight;
    }

    public override void Render(UIRenderer renderer)
    {
        TextureHandle logoTexture = renderer.TextureManager.GetTextureId("/title/mclogo.png");

        // Match legacy rendering logic (split into two textured quads)
        renderer.DrawTexturedModalRect(logoTexture, 0, 0, 0, 0, 155, 44);
        renderer.DrawTexturedModalRect(logoTexture, 155, 0, 0, 45, 155, 44);

        int x = (int)(45 + Math.Sin(Environment.TickCount64 / 100) * 20);
        int y = (int)(Math.Sin(Environment.TickCount64 / 100) * 10);

        renderer.DrawText("BetaSharpest", x, y, Guis.Color.White, 3, false);

        base.Render(renderer);
    }
}
