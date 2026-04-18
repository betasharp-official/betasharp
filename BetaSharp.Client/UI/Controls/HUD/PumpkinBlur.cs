using BetaSharp.Client.Entities;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Items;

namespace BetaSharp.Client.UI.Controls.HUD;

public class PumpkinBlur(Func<ClientPlayerEntity?> getPlayer) : UIElement
{
    public override void Render(UIRenderer renderer)
    {
        ClientPlayerEntity? player = getPlayer();
        if (player == null) return;

        ItemStack? head = player.Inventory.ArmorItemBySlot(3);
        if (head != null && head.ItemId == 86) // Pumpkin
        {
            renderer.DrawTexture(renderer.TextureManager.GetTextureId("%blur%%clamp%/misc/pumpkinblur.png"), 0, 0, ComputedWidth, ComputedHeight);
        }

        base.Render(renderer);
    }
}
