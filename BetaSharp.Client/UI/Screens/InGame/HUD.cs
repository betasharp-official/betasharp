using BetaSharp.Client.UI.Controls;
using BetaSharp.Client.UI.Controls.Achievement;
using BetaSharp.Client.UI.Controls.HUD;
using BetaSharp.Client.UI.Layout.Flexbox;

namespace BetaSharp.Client.UI.Screens.InGame;

public class HUD : UIScreen
{
    public override bool PausesGame => false;
    protected override bool AutoAddTooltipBar => false;

    public Hotbar Hotbar { get; private set; } = null!;
    public ChatOverlay Chat { get; private set; } = null!;
    public AchievementToast AchievementToast { get; private set; } = null!;
    public LicenseWarning LicenseWarning { get; private set; } = null!;

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

        // Overlay elements
        var vignette = new Vignette(Game);
        vignette.Style.Position = PositionType.Absolute;
        vignette.Style.Top = vignette.Style.Left = vignette.Style.Right = vignette.Style.Bottom = 0;
        Root.AddChild(vignette);

        var portal = new PortalOverlay(Game);
        portal.Style.Position = PositionType.Absolute;
        portal.Style.Top = portal.Style.Left = portal.Style.Right = portal.Style.Bottom = 0;
        Root.AddChild(portal);

        var pumpkin = new PumpkinBlur(Game);
        pumpkin.Style.Position = PositionType.Absolute;
        pumpkin.Style.Top = pumpkin.Style.Left = pumpkin.Style.Right = pumpkin.Style.Bottom = 0;
        Root.AddChild(pumpkin);

        Hotbar = new Hotbar(Game);
        Root.AddChild(Hotbar);

        Chat = new ChatOverlay();
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

        // Foreground elements
        var crosshair = new Crosshair();
        crosshair.Style.Position = PositionType.Absolute;
        crosshair.Style.Top = crosshair.Style.Left = crosshair.Style.Right = crosshair.Style.Bottom = 0;
        Root.AddChild(crosshair);

        var debugMenu = new DebugMenu(Game);
        debugMenu.Style.Position = PositionType.Absolute;
        debugMenu.Style.Top = debugMenu.Style.Left = debugMenu.Style.Right = debugMenu.Style.Bottom = 0;
        Root.AddChild(debugMenu);

        var tooltipBar = new ControlTooltipBar(Game);
        tooltipBar.Style.Position = PositionType.Absolute;
        tooltipBar.Style.Bottom = 4;
        tooltipBar.Style.Left = 2;
        tooltipBar.Style.MarginLeft = 16;
        tooltipBar.Style.MarginBottom = 4;
        Root.AddChild(tooltipBar);
    }

    public void AddChatMessage(string message) => Chat.AddMessage(message);

    public override void Update(float partialTicks)
    {
        base.Update(partialTicks);

        LicenseWarning.Visible = BetaSharp.hasPaidCheckTime > 0;
        Game.componentsStorage.Overlay.Context.GCMonitor.AllowUpdating = Game.options.ShowDebugInfo;
    }
}
