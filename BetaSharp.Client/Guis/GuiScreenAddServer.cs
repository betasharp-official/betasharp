using BetaSharp.Client.Guis.Controls;
using BetaSharp.Client.Guis.Layout;
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

        Text = "Edit Server Info";
        DisplayTitle = true;

        Control container = new(EffectiveWidth / 2 - 100, EffectiveHeight / 2 - 80, 200, 160)
        {
            VerticalCenteringBehavior = CenteringBehavior.Middle,
        };

        Label serverNameLabel = new(0, 0, "Server Name", 0xA0A0A0);
        _serverName = new(0, 13, FontRenderer, serverData.Name)
        {
            Focused = true,
            MaxLength = 32,
        };
        Label serverAddressLabel = new(0, 41, "Server Address", 0xA0A0A0);
        _serverAddress = new(0, 53, FontRenderer, serverData.Ip) { MaxLength = 128 };
        _doneButton = new(0, 116, "Done");
        Button cancelButton = new(0, 140, "Cancel");

        _doneButton.Clicked += (_, _) => parentScreen.ConfirmClicked(false, 0);
        cancelButton.Clicked += (_, _) =>
        {
            serverData.Name = _serverName.Text;
            serverData.Ip = _serverAddress.Text;
            parentScreen.ConfirmClicked(true, 0);
        };
        _serverName.KeyInput += FieldKeyPressed;
        _serverAddress.KeyInput += FieldKeyPressed;

        container.AddChildren(serverNameLabel, _serverName, serverAddressLabel, _serverAddress, _doneButton, cancelButton);
        AddChild(container);

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
    }
}
