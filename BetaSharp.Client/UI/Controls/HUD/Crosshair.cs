using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.HUD;

public class Crosshair : UIElement
{
    public override void Render(UIRenderer renderer)
    {
        renderer.TextureManager.BindTexture(renderer.TextureManager.GetTextureId("/gui/icons.png"));
        renderer.PushBlend(UiBlendFactor.OneMinusDstColor, UiBlendFactor.OneMinusSrcColor);
        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), ComputedWidth / 2 - 8, ComputedHeight / 2 - 8, 0, 0, 16, 16);
        renderer.PopBlend();

        base.Render(renderer);
    }
}
