using BetaSharp.Client.UI.Rendering;
using BetaSharp.Items;

namespace BetaSharp.Client.UI.Controls.HUD;

public class PumpkinBlur(BetaSharp game) : UIElement
{
    private readonly BetaSharp _game = game;

    public override void Render(UIRenderer renderer)
    {
        if (_game.player == null) return;

        ItemStack head = _game.player.inventory.armorItemInSlot(3);
        if (head != null && head.itemId == 86) // Pumpkin
        {
            renderer.DrawTexture(renderer.TextureManager.GetTextureId("%blur%%clamp%/misc/pumpkinblur.png"), 0, 0, ComputedWidth, ComputedHeight);
        }

        base.Render(renderer);
    }
}
