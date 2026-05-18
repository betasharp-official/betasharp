using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.MainMenu;

public class MainMenuLogo : UIElement
{
    private const short LogoWidth = 260;
    private const byte LogoHeight = 40;

    public MainMenuLogo()
    {
        Style.Width = LogoWidth;
        Style.Height = LogoHeight;
    }

    public override void Render(UIRenderer renderer)
    {
        TextureHandle logoTexture = renderer.TextureManager.GetTextureId("/gui/BetaSharp.png");

        // Match legacy rendering logic (split into two textured quads)
        renderer.DrawTexture(logoTexture, 0, 0, 256, 64);

        base.Render(renderer);
    }
}
