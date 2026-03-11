using BetaSharp.Client.Input;

namespace BetaSharp.Client.Guis;

public class GuiScreenAddServer : GuiScreen
{
    private readonly GuiMultiplayer _parentScreen;
    private GuiTextField _serverName = null!;
    private GuiTextField _serverAddress = null!;
    private readonly ServerData _serverData;

    public GuiScreenAddServer(GuiMultiplayer parentScreen, ServerData serverData)
    {
        _parentScreen = parentScreen;
        _serverData = serverData;
    }

    public override void UpdateScreen()
    {
        _serverName.updateCursorCounter();
        _serverAddress.updateCursorCounter();
    }

    public override void InitGui()
    {
        Keyboard.enableRepeatEvents(true);
        _controlList.Clear();
        _controlList.Add(new GuiButton(0, Width / 2 - 100, Height / 4 + 96 + 12, "Done"));
        _controlList.Add(new GuiButton(1, Width / 2 - 100, Height / 4 + 120 + 12, "Cancel"));

        _serverName = new GuiTextField(this, FontRenderer, Width / 2 - 100, 66, 200, 20, _serverData.Name)
        {
            IsFocused = true
        };
        _serverName.SetMaxStringLength(32);

        _serverAddress = new GuiTextField(this, FontRenderer, Width / 2 - 100, 106, 200, 20, _serverData.Ip);
        _serverAddress.SetMaxStringLength(128);

        _controlList[0].Enabled = _serverName.GetText().Length > 0 && _serverAddress.GetText().Length > 0 && _serverAddress.GetText().Split(":").Length > 0;
    }

    public override void OnGuiClosed()
    {
        Keyboard.enableRepeatEvents(false);
    }

    protected override void ActionPerformed(GuiButton button)
    {
        if (button.Enabled)
        {
            if (button.Id == 1)
            {
                CloseScreen();
            }
            else if (button.Id == 0)
            {
                _serverData.Name = _serverName.GetText();
                _serverData.Ip = _serverAddress.GetText();
                _parentScreen.ConfirmClicked(true, 0);
            }
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        _serverName.textboxKeyTyped(eventChar, eventKey);
        _serverAddress.textboxKeyTyped(eventChar, eventKey);

        if (eventKey == Keyboard.KEY_TAB)
        {
            if (_serverName.IsFocused)
            {
                _serverName.IsFocused = false;
                _serverAddress.IsFocused = true;
            }
            else
            {
                _serverName.IsFocused = true;
                _serverAddress.IsFocused = false;
            }
        }

        if (eventKey == Keyboard.KEY_RETURN)
        {
            ActionPerformed(_controlList[0]);
        }
        else if (eventKey == Keyboard.KEY_ESCAPE)
        {
            CloseScreen();
        }

        _controlList[0].Enabled = _serverName.GetText().Length > 0 && _serverAddress.GetText().Length > 0 && _serverAddress.GetText().Split(":").Length > 0;
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);
        _serverName.MouseClicked(x, y, button);
        _serverAddress.MouseClicked(x, y, button);
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, "Edit Server Info", Width / 2, 17, Color.White);
        DrawString(FontRenderer, "Server Name", Width / 2 - 100, 53, Color.GrayA0);
        DrawString(FontRenderer, "Server Address", Width / 2 - 100, 94, Color.GrayA0);
        _serverName.DrawTextBox();
        _serverAddress.DrawTextBox();
        base.Render(mouseX, mouseY, partialTicks);
    }

    protected override void CloseScreen()
    {
        Game.sndManager.PlayUISound("", "console.back", Game.isControllerMode);
        _parentScreen.ConfirmClicked(false, 0);
    }
}
