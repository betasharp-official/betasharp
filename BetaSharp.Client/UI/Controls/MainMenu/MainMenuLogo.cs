using BetaSharp.Client.Rendering.Core.Textures;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.MainMenu;

public class MainMenuLogo : UIElement
{
    private const short LogoWidth = 265;
    private const byte LogoHeight = 48;

    public MainMenuLogo()
    {
        Style.Width = LogoWidth;
        Style.Height = LogoHeight;
    }

    public override void Render(UIRenderer renderer)
    {
        TextureHandle logoTexture = renderer.TextureManager.GetTextureId("/gui/BetaSharp.png");

        renderer.DrawTexture(logoTexture, 0, 0, 256, 48);

        base.Render(renderer);
    }
}
