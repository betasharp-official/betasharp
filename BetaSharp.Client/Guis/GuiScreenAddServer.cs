using BetaSharp.Client.Input;

namespace BetaSharp.Client.Guis;

public class GuiScreenAddServer : Screen
{
    private readonly TextField _serverName;
    private readonly TextField _serverAddress;
    private readonly Button _doneButton;

    public GuiScreenAddServer(GuiMultiplayer parentScreen, ServerData serverData)
    {
        Keyboard.enableRepeatEvents(true);
        int buttonLeft = Width / 2 - 100;

        _doneButton = new(buttonLeft, Height / 4 + 96 + 12, "Done");
        Button cancelButton = new(buttonLeft, Height / 4 + 120 + 12, "Cancel");
        _serverName = new(buttonLeft, 66, FontRenderer, serverData.Name)
        {
            Focused = true,
            MaxLength = 32,
        };
        _serverAddress = new(buttonLeft, 106, FontRenderer, serverData.Ip) { MaxLength = 128 };

        _doneButton.Clicked += (_, _) => parentScreen.ConfirmClicked(false, 0);
        cancelButton.Clicked += (_, _) =>
        {
            serverData.Name = _serverName.Text;
            serverData.Ip = _serverAddress.Text;
            parentScreen.ConfirmClicked(true, 0);
        };
        _serverName.KeyInput += FieldKeyPressed;
        _serverAddress.KeyInput += FieldKeyPressed;

        AddChildren(_doneButton, cancelButton, _serverName, _serverAddress);

        _doneButton.Enabled = _serverName.Text.Length > 0 && _serverAddress.Text.Length > 0 && _serverAddress.Text.Split(":").Length > 0;
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    private void FieldKeyPressed(object? sender, KeyboardEventArgs e)
    {
        if (e.Key == Keyboard.KEY_RETURN)
        {
            _doneButton.DoClick((MouseEventArgs)EventArgs.Empty);
        }

        _doneButton.Enabled = _serverName.Text.Length > 0 && _serverAddress.Text.Length > 0 && _serverAddress.Text.Split(":").Length > 0;
    }

    protected override void OnRender(RenderEventArgs e)
    {
        DrawDefaultBackground();
        Gui.DrawCenteredString(FontRenderer, "Edit Server Info", Width / 2, 17, 0xFFFFFF);
        Gui.DrawString(FontRenderer, "Server Name", Width / 2 - 100, 53, 0xA0A0A0);
        Gui.DrawString(FontRenderer, "Server Address", Width / 2 - 100, 94, 0xA0A0A0);
    }
}
