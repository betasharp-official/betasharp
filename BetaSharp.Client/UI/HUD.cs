using BetaSharp.Client.Guis;
using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Layout.Flexbox;
using BetaSharp.Client.UI.Rendering;
using BetaSharp.Items;
using GLEnum = BetaSharp.Client.Rendering.Core.OpenGL.GLEnum;

namespace BetaSharp.Client.UI;

public class HUD : UIScreen
{
    public override bool PausesGame => false;

    public Hotbar Hotbar { get; private set; } = null!;
    public ChatOverlay Chat { get; private set; } = null!;
    public AchievementToast AchievementToast { get; private set; } = null!;
    public LicenseWarning LicenseWarning { get; private set; } = null!;
    private float _prevVignetteBrightness = 1.0f;

    public HUD(BetaSharp game) : base(game)
    {
        Initialize();
    }

    protected override void Init()
    {
        Root.Style.Width = null;
        Root.Style.Height = null;
        Root.Style.JustifyContent = Justify.FlexEnd;
        Root.Style.AlignItems = Align.Center;

        Hotbar = new Hotbar(Game);
        Root.AddChild(Hotbar);

        Chat = new ChatOverlay(Game);
        Chat.Style.Position = PositionType.Absolute;
        Chat.Style.Bottom = 48;
        Chat.Style.Left = 2;
        Root.AddChild(Chat);

        AchievementToast = new AchievementToast(Game);
        AchievementToast.Style.Position = PositionType.Absolute;
        AchievementToast.Style.Top = 0;
        AchievementToast.Style.Right = 0;
        Root.AddChild(AchievementToast);

        LicenseWarning = new LicenseWarning(Game);
        LicenseWarning.Style.Position = PositionType.Absolute;
        LicenseWarning.Style.Top = 2;
        LicenseWarning.Style.Left = 2;
        Root.AddChild(LicenseWarning);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        Renderer.Begin();
        RenderVignette(Renderer, partialTicks);
        RenderPortalOverlay(Renderer, partialTicks);
        RenderPumpkinBlur(Renderer);
        Renderer.End();

        base.Render(mouseX, mouseY, partialTicks);

        Renderer.Begin();
        RenderCrosshair(Renderer);
        Renderer.End();
    }

    public void AddChatMessage(string message) => Chat.AddMessage(message);


    private void RenderVignette(UIRenderer renderer, float partialTicks)
    {
        float darkness = Game.player.getBrightnessAtEyes(partialTicks);
        darkness = 1.0F - darkness;
        if (darkness < 0.0F) darkness = 0.0F;
        if (darkness > 1.0F) darkness = 1.0F;

        _prevVignetteBrightness = (float)(_prevVignetteBrightness + (double)(darkness - _prevVignetteBrightness) * 0.01D);

        renderer.SetAlphaTest(false);
        renderer.SetDepthMask(false);

        renderer.PushBlend(GLEnum.Zero, GLEnum.OneMinusSrcColor);
        renderer.PushColor(new Color((byte)(255 * _prevVignetteBrightness), (byte)(255 * _prevVignetteBrightness), (byte)(255 * _prevVignetteBrightness), 255));

        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("%blur%%clamp%/misc/vignette.png"), 0, 0, 0, 0, Root.ComputedWidth, Root.ComputedHeight, 256, 256, -90.0f);

        renderer.PopColor();
        renderer.PopBlend();
        renderer.SetDepthMask(true);
        renderer.SetAlphaTest(true);
    }

    private void RenderPortalOverlay(UIRenderer renderer, float partialTicks)
    {
        float last = Game.player.lastScreenDistortion;
        float curr = Game.player.changeDimensionCooldown;
        float portal = last + (curr - last) * partialTicks;

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

            renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/terrain.png"), 0, 0, 14 * 16, 0 * 16, Root.ComputedWidth, Root.ComputedHeight, 16, 16, -90.0f);
            renderer.PopColor();
            renderer.SetDepthMask(true);
            renderer.SetAlphaTest(true);
        }
    }

    private void RenderPumpkinBlur(UIRenderer renderer)
    {
        ItemStack head = Game.player.inventory.armorItemInSlot(3);
        if (head != null && head.itemId == 86) // Pumpkin
        {
            renderer.DrawTexture(renderer.TextureManager.GetTextureId("%blur%%clamp%/misc/pumpkinblur.png"), 0, 0, Root.ComputedWidth, Root.ComputedHeight);
        }
    }

    private void RenderCrosshair(UIRenderer renderer)
    {
        renderer.TextureManager.BindTexture(renderer.TextureManager.GetTextureId("/gui/icons.png"));
        renderer.PushBlend(GLEnum.OneMinusDstColor, GLEnum.OneMinusSrcColor);
        renderer.DrawTexturedModalRect(renderer.TextureManager.GetTextureId("/gui/icons.png"), Root.ComputedWidth / 2 - 8, Root.ComputedHeight / 2 - 8, 0, 0, 16, 16);
        renderer.PopBlend();
    }

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);

        LicenseWarning.Visible = BetaSharp.hasPaidCheckTime > 0;
    }
}
