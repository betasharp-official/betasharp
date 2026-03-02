using BetaSharp.Client.Input;
using BetaSharp.Client.Options;

namespace BetaSharp.Client.Guis;

public class GuiDirectConnect : GuiScreen
{
    private readonly GuiMultiplayer _parentScreen;
    private GuiTextField _serverAddress = null!;
    private readonly ServerData _serverData;
    private readonly GameOptions _options;

    public GuiDirectConnect(GuiMultiplayer parentScreen, ServerData serverData, GameOptions options)
    {
        _parentScreen = parentScreen;
        _serverData = serverData;
        _options = options;
    }

    public override void UpdateScreen()
    {
        _serverAddress.updateCursorCounter();
    }

    public override void InitGui()
    {
        Keyboard.enableRepeatEvents(true);
        _controlList.Clear();
        _controlList.Add(new GuiButton(0, Width / 2 - 100, Height / 4 + 96 + 12, "Join Server"));
        _controlList.Add(new GuiButton(1, Width / 2 - 100, Height / 4 + 120 + 12, "Cancel"));

        _serverAddress = new GuiTextField(this, FontRenderer, Width / 2 - 100, 106, 200, 20, _serverData.Ip);
        _serverAddress.SetMaxStringLength(128);
        _serverAddress.SetText(_options.LastServer);

        _controlList[0].Enabled = _serverAddress.GetText().Length > 0 && _serverAddress.GetText().Split(":").Length > 0;
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
                _parentScreen.ConfirmClicked(false, 0);
            }
            else if (button.Id == 0)
            {
                _serverData.Ip = _serverAddress.GetText();
                _parentScreen.ConfirmClicked(true, 0);
            }
        }
    }

    protected override void KeyTyped(char eventChar, int eventKey)
    {
        _serverAddress.textboxKeyTyped(eventChar, eventKey);

        if (eventKey == Keyboard.KEY_TAB)
        {
            _serverAddress.IsFocused = true;
        }

        if (eventKey == Keyboard.KEY_RETURN)
        {
            ActionPerformed(_controlList[0]);
        }

        _controlList[0].Enabled = _serverAddress.GetText().Length > 0 && _serverAddress.GetText().Split(":").Length > 0;
    }

    protected override void MouseClicked(int x, int y, int button)
    {
        base.MouseClicked(x, y, button);

        _serverAddress.MouseClicked(x, y, button);

        _options.LastServer = _serverAddress.GetText();
        _options.SaveOptions();
    }

    public override void Render(int mouseX, int mouseY, float partialTicks)
    {
        DrawDefaultBackground();
        DrawCenteredString(FontRenderer, "Direct Connect", Width / 2, 17, Color.White);
        DrawString(FontRenderer, "Server Address", Width / 2 - 100, 94, Color.GrayA0);
        _serverAddress.DrawTextBox();
        base.Render(mouseX, mouseY, partialTicks);
    }
}
