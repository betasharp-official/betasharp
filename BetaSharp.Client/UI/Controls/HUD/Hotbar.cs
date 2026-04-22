using BetaSharp.Blocks.Materials;
using BetaSharp.Client.Entities;
using BetaSharp.Client.Input;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Util.Maths;

namespace BetaSharp.Client.UI.Controls.HUD;

public class Hotbar : UIElement
{
    private readonly Func<ClientPlayerEntity?> _getPlayer;
    private readonly Func<PlayerController?> _getPlayerController;
    private readonly IControllerState _controllerState;
    private readonly JavaRandom _rand = new();
    private int _updateCounter = 0;

    public Hotbar(
        Func<ClientPlayerEntity?> getPlayer,
        Func<PlayerController?> getPlayerController,
        IControllerState controllerState)
    {
        _getPlayer = getPlayer;
        _getPlayerController = getPlayerController;
        _controllerState = controllerState;
        Style.Width = 182;
        Style.Height = 22;
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        Style.MarginBottom = _controllerState.IsControllerMode ? 28 : 0;
        _updateCounter++;
    }

    public override void Render(UIRenderer renderer)
    {
        ClientPlayerEntity? player = _getPlayer();
        if (player == null) return;

        // --- 1. Background (Hotbar itself) ---
        renderer.TextureManager.BindTexture(renderer.TextureManager.GetTextureId("/gui/gui.png"));
        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/gui.png"), 0, 0, 0, 0, 182, 22);

        // Selection highlight
        InventoryPlayer inventory = player.inventory;
        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/gui.png"), inventory.SelectedSlot * 20 - 1, -1, 0, 22, 24, 22);

        RenderStats(renderer);

        for (int i = 0; i < 9; ++i)
        {
            RenderSlot(renderer, i, i * 20 + 3, 3);
        }

        base.Render(renderer);
    }

    private void RenderStats(UIRenderer renderer)
    {
        ClientPlayerEntity? player = _getPlayer();
        if (player == null) return;
        if (!(_getPlayerController()?.shouldDrawHUD() ?? false)) return;

        renderer.TextureManager.BindTexture(renderer.TextureManager.GetTextureId("/gui/icons.png"));

        int armorValue = player.getPlayerArmorValue();
        int health = player.Health;
        int lastHealth = player.LastHealth;
        bool heartBlink = player.Hearts / 3 % 2 == 1 && player.Hearts >= 10;

        _rand.SetSeed(_updateCounter * 312871);

        for (int i = 0; i < 10; ++i)
        {
            int statY = -10; // Relative to hotbar top

            // --- Armor ---
            if (armorValue > 0)
            {
                int armorX = 173 - i * 8; // Offset from right
                if (i * 2 + 1 < armorValue) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), armorX, statY, 34, 9, 9, 9);
                else if (i * 2 + 1 == armorValue) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), armorX, statY, 25, 9, 9, 9);
                else renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), armorX, statY, 16, 9, 9, 9);
            }

            // --- Health ---
            if (!player.GameMode.CanReceiveDamage) continue;

            int healthX = i * 8;
            int healthY = statY;
            if (health <= 4) healthY += _rand.NextInt(2);

            byte blinkIndex = (byte)(heartBlink ? 1 : 0);
            // BG
            renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), healthX, healthY, 16 + blinkIndex * 9, 0, 9, 9);
            // Blink overlay
            if (heartBlink)
            {
                if (i * 2 + 1 < lastHealth) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), healthX, healthY, 70, 0, 9, 9);
                else if (i * 2 + 1 == lastHealth) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), healthX, healthY, 79, 0, 9, 9);
            }
            // Fill
            if (i * 2 + 1 < health) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), healthX, healthY, 52, 0, 9, 9);
            else if (i * 2 + 1 == health) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), healthX, healthY, 61, 0, 9, 9);
        }

        // --- Air ---
        if (player.IsInFluid(Material.Water) && player.GameMode.NeedsAir)
        {
            int air = player.Air;
            int fullBubbles = (int)Math.Ceiling((air - 2) * 10.0D / 300.0D);
            int partialBubbles = (int)Math.Ceiling(air * 10.0D / 300.0D) - fullBubbles;

            for (int k = 0; k < fullBubbles + partialBubbles; ++k)
            {
                if (k < fullBubbles) renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), k * 8, -19, 16, 18, 9, 9);
                else renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), k * 8, -19, 25, 18, 9, 9);
            }
        }
    }

    private void RenderSlot(UIRenderer renderer, int slotIndex, int x, int y)
    {
        ItemStack? stack = _getPlayer()?.inventory.Main[slotIndex];
        if (stack == null) return;

        renderer.DrawItem(stack, x, y);
        renderer.DrawItemOverlay(stack, x, y);
    }
}
