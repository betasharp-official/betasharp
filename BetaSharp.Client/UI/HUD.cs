using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Layout;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Inventorys;
using BetaSharp.Items;
using BetaSharp.Client.UI.Rendering;
using UIUIRenderer = BetaSharp.Client.UI.Rendering.UIRenderer;

namespace BetaSharp.Client.UI;

public class HUD : UIScreen
{
    public HUD(BetaSharp game) : base(game)
    {
        Initialize();
    }
    public override bool PausesGame => false;

    public Hotbar Hotbar { get; private set; } = null!;
    public ChatOverlay Chat { get; private set; } = null!;
    public AchievementToast AchievementToast { get; private set; } = null!;
    public LicenseWarning LicenseWarning { get; private set; } = null!;

    public void AddChatMessage(string message) => Chat.AddMessage(message);
    public void AddChatMessageTranslate(string key) => Chat.AddMessage(key); // Simplified for now

    protected override void Init()
    {
        Root.Style.Width = null;
        Root.Style.Height = null;
        Root.Style.JustifyContent = Justify.FlexEnd; // Push to bottom by default for hotbar
        Root.Style.AlignItems = Align.Center;

        // Hotbar at the bottom
        Hotbar = new Hotbar(Game);
        Root.AddChild(Hotbar);

        // Chat overlay (usually bottom left, above hotbar)
        Chat = new ChatOverlay(Game);
        Chat.Style.Position = PositionType.Absolute;
        Chat.Style.Bottom = 48;
        Chat.Style.Left = 2;
        Root.AddChild(Chat);

        // Achievement Toast (top right)
        AchievementToast = new AchievementToast(Game);
        AchievementToast.Style.Position = PositionType.Absolute;
        AchievementToast.Style.Top = 0;
        AchievementToast.Style.Right = 0;
        Root.AddChild(AchievementToast);

        // License Warning (top left)
        LicenseWarning = new LicenseWarning(Game);
        LicenseWarning.Style.Position = PositionType.Absolute;
        LicenseWarning.Style.Top = 2;
        LicenseWarning.Style.Left = 2;
        Root.AddChild(LicenseWarning);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        // 1. Pre-UI effects (Vignette, etc.)
        Renderer.Begin();
        RenderVignette(Renderer, partialTicks);
        RenderPortalOverlay(Renderer, partialTicks);
        RenderPumpkinBlur(Renderer);
        Renderer.End();

        // 2. Main UI
        base.Render(mouseX, mouseY, partialTicks);

        // 3. Post-UI (Crosshair, Tooltips)
        Renderer.Begin();
        RenderCrosshair(Renderer);
        Renderer.End();
    }

    private void RenderVignette(UIUIRenderer renderer, float partialTicks)
    {
        float darkness = Game.player.getBrightnessAtEyes(partialTicks);
        darkness = 1.0F - darkness;
        if (darkness < 0.0F) darkness = 0.0F;
        if (darkness > 1.0F) darkness = 1.0F;

        // Note: Legacy GuiIngame uses a smoothed _prevVignetteBrightness. 
        // For now we'll just use the raw darkness or add a field if needed.
        if (darkness > 0.0f)
        {
            renderer.PushColor(new Color(0, 0, 0, (byte)(255 * darkness)));
            renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/vignette.png"), 0, 0, 0, 0, Root.ComputedWidth, Root.ComputedHeight);
            renderer.PopColor();
        }
    }

    private void RenderPortalOverlay(UIUIRenderer renderer, float partialTicks)
    {
        float portal = Game.player.changeDimensionCooldown; // changeDimensionCooldown is the portal timer
        if (portal > 0.0F)
        {
            if (portal < 1.0F)
            {
                portal *= portal;
                portal *= portal;
                portal = portal * 0.8F + 0.2F;
            }

            renderer.PushColor(new Color(255, 255, 255, (byte)(255 * portal)));
            renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/terrain.png"), 0, 0, 0, 0, Root.ComputedWidth, Root.ComputedHeight);
            renderer.PopColor();
        }
    }

    private void RenderPumpkinBlur(UIUIRenderer renderer)
    {
        ItemStack head = Game.player.inventory.armorItemInSlot(3);
        if (head != null && head.itemId == 86) // Pumpkin
        {
            renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/pumpkinblur.png"), 0, 0, 0, 0, Root.ComputedWidth, Root.ComputedHeight);
        }
    }

    private void RenderCrosshair(UIUIRenderer renderer)
    {
        renderer.TextureManager.BindTexture(renderer.TextureManager.GetTextureId("/gui/icons.png"));
        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), Root.ComputedWidth / 2 - 7, Root.ComputedHeight / 2 - 7, 0, 0, 16, 16);
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);
        
        // Update visibility based on game state
        LicenseWarning.Visible = BetaSharp.hasPaidCheckTime > 0;
    }
}
