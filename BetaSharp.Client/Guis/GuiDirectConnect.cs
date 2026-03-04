using BetaSharp.Client.Input;

namespace BetaSharp.Client.Guis;

public class GuiDirectConnect : Screen
{
    private readonly GuiMultiplayer _parentScreen;
    private readonly TextField _serverAddress;
    private readonly ServerData _serverData;

    public GuiDirectConnect(GuiMultiplayer parentScreen, ServerData serverData)
    {
        _parentScreen = parentScreen;
        _serverData = serverData;

        Keyboard.enableRepeatEvents(true);

        _serverAddress = new(Width / 2 - 100, 106, FontRenderer, _serverData.Ip)
        {
            Anchor = Anchors.Top,
            MaxLength = 128,
        };
        Button joinServerButton = new(Width / 2 - 100, Height / 4 + 96 + 12, "Join Server")
        {
            Enabled = _serverAddress.Text.Length > 0 && _serverAddress.Text.Split(":").Length > 0,
        };
        Button cancelButton = new(Width / 2 - 100, Height / 4 + 120 + 12, "Cancel");
        joinServerButton.Clicked += (_, _) =>
        {
            _serverData.Ip = _serverAddress.Text;
            _parentScreen.ConfirmClicked(true, 0);
        };
        cancelButton.Clicked += (_, _) => _parentScreen.ConfirmClicked(false, 0);
        _serverAddress.TextChanged += (_, _) =>
        {
            joinServerButton.Enabled = _serverAddress.Text.Length > 0 && _serverAddress.Text.Split(":").Length > 0;
        };

        AddChildren(joinServerButton, cancelButton, _serverAddress);
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, "Direct Connect", Width / 2, 17, 0xFFFFFF);
        Gui.DrawString(FontRenderer, "Server Address", Width / 2 - 100, 94, 0xA0A0A0);
    }
}
