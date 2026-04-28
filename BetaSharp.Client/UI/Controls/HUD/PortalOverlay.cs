using BetaSharp.Client.Entities;
using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Rendering;

namespace BetaSharp.Client.UI.Controls.HUD;

public class PortalOverlay(Func<ClientPlayerEntity?> getPlayer) : UIElement
{
    private float _partialTicks;

    public override void Update(float partialTicks)
    {
        _partialTicks = partialTicks;
        base.Update(partialTicks);
    }

    public override void Render(UIRenderer renderer)
    {
        ClientPlayerEntity? player = getPlayer();
        if (player == null) return;

        float last = player.LastScreenDistortion;
        float curr = player.ChangeDimensionCooldown;
        float portal = last + (curr - last) * _partialTicks;

        if (portal > 0.0F)
        {
            if (portal < 1.0F)
            {
                portal *= portal;
                portal *= portal;
                portal = portal * 0.8F + 0.2F;
            }

            renderer.SetAlphaTest(false);
            renderer.SetDepthMask(false);
            renderer.PushColor(new Color(255, 255, 255, (byte)(255 * portal)));

            renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/terrain.png"), 0, 0, 14 * 16, 0 * 16, ComputedWidth, ComputedHeight, 16, 16, -90.0f);
            renderer.PopColor();
            renderer.SetDepthMask(true);
            renderer.SetAlphaTest(true);
        }

        base.Render(renderer);
    }
}
